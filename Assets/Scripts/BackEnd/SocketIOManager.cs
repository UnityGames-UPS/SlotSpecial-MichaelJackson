using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField] private string testToken = "test-token";
    protected string testSocketURL = "https://devrealtime.dingdinghouse.com/";
    protected string nameSpace = "playground";
    protected string gameID = "SL-MJ";

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private GameObject RaycastBlocker;

    private SocketManager socketManager;
    private Socket gameSocket;

    private string authToken;
    private string socketURL;

    internal bool isConnected;
    internal bool isInitialized;
    internal bool isExiting;
    private bool isBeingDestroyed;

    private bool hasFocus = true;
    private float focusLostTime;
    private Coroutine focusCheckRoutine;
    private const float MAX_BACKGROUND_TIME = 60f;

    private Coroutine pingCoroutine;
    private float lastPongTime;
    private bool waitingForPong;
    private int missedPongs;
    private const int MAX_MISSED_PONGS = 15;
    private const float PING_INTERVAL = 2f;
    private const float PONG_TIMEOUT = 5f;

    // FIX: track whether auth arrived before Start() ran
    private bool authReceivedBeforeStart = false;
    private string pendingAuthJson = null;

    #region Initialization

    private void Awake()
    {
        isInitialized = false;
        isConnected = false;
        isExiting = false;
    }

    private void Start()
    {
        RequestAuthToken();
    }

    private void RequestAuthToken()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            // FIX: If platform already called ReceiveAuthToken before Start() ran,
            // the pendingAuthJson is waiting — process it now instead of asking again.
            if (pendingAuthJson != null)
            {
                Debug.Log("[SocketIO] Auth was buffered before Start — processing now");
                ProcessAuthData(pendingAuthJson);
                pendingAuthJson = null;
            }
            else
            {
                // Normal path: ask platform for token, it will call ReceiveAuthToken
                JSManager.RegisterAuthTokenListener(gameObject.name); // listen for host's TokenReceived before asking
                JSManager.SendCustomMessage("authToken");
            }
        }
#else
        authToken = testToken;
        socketURL = testSocketURL;
        InitializeSocket();
#endif
    }

    // Called by platform via Unity SendMessage('SocketManager', 'ReceiveAuthToken', json)
    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("[SocketIO] Auth received");

        // FIX: If Start() hasn't run yet (scene still loading), buffer the data.
        // RequestAuthToken() will process it when Start() fires.
        if (!isActiveAndEnabled || socketManager != null)
        {
            // Already initialized or not yet started — buffer it
            pendingAuthJson = jsonData;
            Debug.Log("[SocketIO] Auth buffered (scene not ready yet)");
            return;
        }

        ProcessAuthData(jsonData);
    }

    private void ProcessAuthData(string jsonData)
    {
        try
        {
            var authData = JsonUtility.FromJson<AuthTokenData>(jsonData);

            authToken = authData.cookie;
            socketURL = authData.socketURL;

            if (!string.IsNullOrEmpty(authData.nameSpace))
            {
                nameSpace = authData.nameSpace;
            }

            InitializeSocket();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Auth parse failed: {e.Message}");
        }
    }

    private void InitializeSocket()
    {
        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        SocketOptions options = new SocketOptions
        {
            AutoConnect = false,
            Reconnection = false,
            Timeout = TimeSpan.FromSeconds(3),
            ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
        };

        options.Auth = (SocketManager manager, Socket socket) => new { token = authToken };

#if UNITY_EDITOR
        socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
        socketManager = new SocketManager(new Uri(socketURL), options);
#endif

        gameSocket = string.IsNullOrEmpty(nameSpace)
            ? socketManager.Socket
            : socketManager.GetSocket("/" + nameSpace);

        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        gameSocket.On<string>("game:init", OnInitReceived);
        gameSocket.On<string>("result", OnResultReceived);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On<string>("AnotherDevice", OnAnotherDevice);
        gameSocket.On<string>("balance:sync", OnBalanceSync);

        socketManager.Open();
    }

    #endregion

    #region Socket Events

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("New Build");
        Debug.Log("[SocketIO] Connected");

        isConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;

        if (popupManager != null)
        {
            popupManager.HideReconnectionPopup();
        }

        StartPingRoutine();
    }

    private void OnSocketDisconnected()
    {
        Debug.Log("[SocketIO] Disconnected");

        isConnected = false;
        StopPingRoutine();

        if (isExiting)
        {
            if (popupManager != null)
            {
                popupManager.ShowLoadingPopup(0f);
            }
        }
        else
        {
            if (popupManager != null)
            {
                popupManager.ShowDisconnectionPopup();
            }

            if (gameManager != null)
            {
                gameManager.OnDisconnected();
            }
        }
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError($"[SocketIO] Error: {err.message}");

        if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
        {
            Debug.LogWarning("[SocketIO] Session expired detected");

            if (popupManager != null)
            {
                popupManager.ShowSessionExpiredError();
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("session_expired");
            }
#endif
        }
        else
        {
            if (!gameManager.isInitialized)
            {
                gameManager.initializationFailed = true;
            }

            if (popupManager != null)
            {
                popupManager.ShowServerError(err.message);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("error");
            }
#endif
        }
    }

    private void OnBalanceSync(string data)
    {
        BalanceSyncPayload syncPayload = JsonConvert.DeserializeObject<BalanceSyncPayload>(data);
        if (syncPayload == null) return;

        gameManager.playerData.balance = syncPayload.balance;
        uiManager.UpdateBalanceDisplay(syncPayload.balance);

        if (!gameManager.CanAffordBet() && popupManager != null)
        {
            popupManager.ShowInsufficientFundsError();
        }
    }

    private void OnInitReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Init received: {jsonData}");

        try
        {
            var initData = JsonConvert.DeserializeObject<InitData>(jsonData);
            var gameConfig = InitDataConverter.ConvertToGameConfig(initData);
            var playerData = InitDataConverter.ConvertToPlayerData(initData.player);
            var initialMatrix = GenerateRandomMatrix();

            isInitialized = true;

            gameManager.OnInitDataReceived(gameConfig, playerData, initialMatrix);

            if (RaycastBlocker) RaycastBlocker.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("OnEnter");
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Init parse failed: {e.Message}");
            gameManager.initializationFailed = true;
            if (popupManager != null)
            {
                popupManager.ShowServerError("Failed to parse game initialization data.");
            }
        }
    }

    private void OnResultReceived(string jsonData)
    {
        if (!jsonData.Contains("\"id\":\"ResultData\""))
        {
            return;
        }

        Debug.Log($"[SocketIO] Result received: {jsonData}");

        try
        {
            var serverResponse = JsonConvert.DeserializeObject<ServerSpinResponse>(jsonData);

            if (!serverResponse.success)
            {
                Debug.LogError("[SocketIO] Spin failed");
                return;
            }

            double currentBalance = gameManager.playerData.balance;
            double betAmount = gameManager.currentBetAmount;
            GameConfig gameConfig = gameManager.gameConfig;

            SpinResult result = InitDataConverter.ConvertServerResponseToSpinResult(
                serverResponse,
                currentBalance,
                betAmount,
                gameConfig
            );

            result.playerData.currentBetIndex = gameManager.currentBetIndex;

            gameManager.OnSpinResultReceived(result);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Result parse failed: {e.Message}");
        }
    }

    private void OnAnotherDevice(string data)
    {
        Debug.Log("[SocketIO] Another device login");

        if (popupManager != null)
        {
            popupManager.ShowAnotherDeviceError();
        }
    }

    #endregion

    internal void SetRaycastBlocker(bool active)
    {
        if (RaycastBlocker != null) RaycastBlocker.SetActive(active);
    }

    #region Focus Timeout

    internal void HandleFocusChange(bool focus)
    {
        hasFocus = focus;

        if (!focus)
        {
            focusLostTime = Time.time;
            if (focusCheckRoutine == null && !isExiting && !isBeingDestroyed)
                focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
        }
        else
        {
            if (focusCheckRoutine != null)
            {
                StopCoroutine(focusCheckRoutine);
                focusCheckRoutine = null;
            }
        }
    }

    private IEnumerator FocusTimeoutCheck()
    {
        while (!hasFocus && !isExiting && !isBeingDestroyed)
        {
            if (Time.time - focusLostTime >= MAX_BACKGROUND_TIME)
            {
                Debug.LogWarning("[SocketIO] Background timeout - closing connection");
                isConnected = false;
                StopPingRoutine();

                if (socketManager != null)
                {
                    try { socketManager.Close(); }
                    catch (Exception e) { Debug.LogWarning($"[SocketIO] Focus close error: {e.Message}"); }
                }

                if (popupManager != null)
                {
                    popupManager.ShowDisconnectionPopup();
                }

                focusCheckRoutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        focusCheckRoutine = null;
    }

    #endregion

    #region Ping/Pong Health Check

    private void StartPingRoutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);

        pingCoroutine = StartCoroutine(PingRoutine());
    }

    private void StopPingRoutine()
    {
        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
            pingCoroutine = null;
        }
    }

    private IEnumerator PingRoutine()
    {
        while (isConnected)
        {
            yield return new WaitForSeconds(PING_INTERVAL);

            if (waitingForPong)
            {
                float timeSinceLastPong = Time.time - lastPongTime;

                if (timeSinceLastPong > PONG_TIMEOUT)
                {
                    missedPongs++;

                    if (missedPongs >= MAX_MISSED_PONGS)
                    {
                        Debug.LogWarning("[SocketIO] Max pongs missed - disconnecting");
                        OnSocketDisconnected();
                        yield break;
                    }

                    if (missedPongs >= 1 && popupManager != null)
                    {
                        popupManager.ShowReconnectionPopup(missedPongs, MAX_MISSED_PONGS);
                    }
                }
            }

            SendPing();
            waitingForPong = true;
        }
    }

    private void SendPing()
    {
        if (gameSocket != null && isConnected)
        {
            gameSocket.Emit("ping");
        }
    }

    private void OnPongReceived(string data)
    {
        waitingForPong = false;
        lastPongTime = Time.time;

        if (missedPongs > 0)
        {
            missedPongs = 0;

            if (popupManager != null)
            {
                popupManager.HideReconnectionPopup();
            }
        }
    }

    #endregion

    #region Spin Request

    internal void SendSpinRequest(int betIndex, int spins = 100)
    {
        Debug.Log($"[SocketIO] Spin request: betIndex={betIndex}, spins={spins}");

        var request = new SpinRequest
        {
            type = "SPIN",
            payload = new SpinPayload
            {
                betIndex = betIndex,
                spins = spins
            }
        };

        string json = JsonUtility.ToJson(request);
        gameSocket.Emit("request", json);
    }

    #endregion

    #region Cleanup

    internal void CloseSocket()
    {
        isExiting = true;

        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        StopPingRoutine();

        if (socketManager != null)
        {
            socketManager.Close();
            socketManager = null;
        }

        isConnected = false;

        // if (popupManager != null && !popupManager.IsLoadingPopupActive())
        // {
        //     popupManager.ShowLoadingPopup(0f);
        // }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("OnExit");
        }
#endif
    }

    private void OnDisable()
    {
        StopPingRoutine();
    }

    private void OnDestroy()
    {
        isBeingDestroyed = true;
        CloseSocket();
    }

    #endregion

    private List<List<int>> GenerateRandomMatrix()
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < 3; row++)
            {
                column.Add(UnityEngine.Random.Range(1, 11));
            }
            matrix.Add(column);
        }
        return matrix;
    }
}

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}