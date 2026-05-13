using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] internal UIManager uiManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private SlotView slotView;

    [Header("Spin Settings")]
    [SerializeField] private float normalSpinDuration = 3.5f;

    internal GameConfig gameConfig;
    internal PlayerData playerData;
    internal SpinResult lastResult;

    internal GameState currentState;

    internal int currentBetIndex;
    internal double currentBetAmount;

    internal bool isAutoPlaying;

    internal bool isInFreeSpins;
    internal int freeSpinsRemaining;
    internal int freeSpinsUsed;
    internal bool waitingForFreeSpinStart;

    internal bool isInitialized;
    internal bool initializationFailed;

    private Coroutine spinCoroutine;
    private bool stopRequested;
    private bool waitingForSpecialWin;

    #region Initialization

    private void Start()
    {
        currentState = GameState.Initializing;
        waitingForFreeSpinStart = false;
        isInitialized = false;
        initializationFailed = false;
    }

    internal void OnInitDataReceived(GameConfig config, PlayerData player, List<List<int>> initialMatrix)
    {
        gameConfig = config;
        playerData = player;
        currentBetIndex = playerData.currentBetIndex;
        UpdateBetAmount();

        if (initialMatrix != null && slotView != null)
        {
            slotView.SetInitialMatrix(initialMatrix);
        }

        isInitialized = true;
        currentState = GameState.Idle;

        uiManager.OnGameInitialized();
    }

    #endregion

    #region Bet Management

    internal void IncreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;
        SetBetIndex((currentBetIndex + 1) % gameConfig.availableBets.Count);
    }

    internal void DecreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;
        SetBetIndex((currentBetIndex - 1 + gameConfig.availableBets.Count) % gameConfig.availableBets.Count);
    }

    internal void SetBetIndex(int index)
    {
        currentBetIndex = index;
        UpdateBetAmount();
        uiManager.UpdateBetDisplay();
    }

    private void UpdateBetAmount()
    {
        currentBetAmount = gameConfig.availableBets[currentBetIndex];
    }

    #endregion

    #region Spin Control
    
    internal void RequestSpin()
    {
        if (waitingForFreeSpinStart) return;

        if (currentState != GameState.Idle) return;
        if (!socketManager.isConnected) return;

        // For now, total bet = bet per line * number of paylines
        double totalBet = currentBetAmount * gameConfig.paylineCount;
        if (!isInFreeSpins && playerData.balance < totalBet)
        {
            if (popupManager != null)
            {
                popupManager.ShowInsufficientFundsError();
            }
            return;
        }

        StartSpin();
    }

    /// <summary>
    /// Called by the stop button to request early reel stop.
    /// </summary>
    internal void RequestStopSpin()
    {
        if (currentState == GameState.Spinning && !isInFreeSpins)
        {
            stopRequested = true;
        }
    }

    /// <summary>
    /// Toggles autoplay on/off. Called by the autoplay button.
    /// </summary>
    internal void ToggleAutoPlay()
    {
        if (isAutoPlaying)
        {
            StopAutoPlay();
        }
        else
        {
            StartAutoPlay();
        }
    }

    private void StartSpin()
    {
        if (lastResult != null)
        {
            ProcessSpinResult();
        }

        lastResult = null;
        currentState = GameState.Spinning;
        stopRequested = false;

        uiManager.OnSpinStarted();

        if (slotView != null)
        {
            slotView.StartSpin();
        }

        socketManager.SendSpinRequest(currentBetIndex);

        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        float elapsed = 0f;

        while (elapsed < normalSpinDuration && !stopRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (lastResult == null)
        {
            yield return null;
        }

        currentState = GameState.Stopping;

        if (slotView != null && lastResult.resultMatrix != null)
        {
            if (stopRequested)
            {
                slotView.QuickStop(lastResult.resultMatrix);
                yield return new WaitForSeconds(0.5f);
                OnReelsStoppedComplete();
            }
            else
            {
                slotView.StopSpin(lastResult.resultMatrix, OnReelsStoppedComplete);
            }
        }
        else
        {
            OnReelsStoppedComplete();
        }
    }

    private void OnReelsStoppedComplete()
    {
        // TODO: When wheel bonus is implemented, check wheelBonusTriggered here
        // if (lastResult.wheelBonusTriggered) { StartWheelBonus(); return; }

        if (lastResult.winAmount > 0 && lastResult.winLines != null && lastResult.winLines.Count > 0)
        {
            double totalBet = currentBetAmount * gameConfig.paylineCount;
            double multiplier = totalBet > 0 ? (lastResult.winAmount / totalBet) : 0;

            if (multiplier >= 5)
            {
                uiManager.DisableControlsDuringWinAnimation();
                currentState = GameState.Idle;
            }
            else
            {
                // For normal wins, trigger UI update immediately and enable controls
                uiManager.OnSpinStopping(lastResult);
                uiManager.EnableControlsAfterWinAnimation();
                uiManager.OnSpinCompleted(lastResult);
                currentState = GameState.Idle;
            }

            slotView.ShowWinLineAnimation(lastResult.winLines, OnWinAnimationComplete);
            StartCoroutine(TriggerWinPopupWithDelay(1.5f, lastResult));
        }
        else
        {
            uiManager.OnSpinStopping(lastResult);
            currentState = GameState.Idle;
            OnWinAnimationComplete();
        }
    }

    private IEnumerator TriggerWinPopupWithDelay(float delay, SpinResult result)
    {
        double totalBet = currentBetAmount * gameConfig.paylineCount;
        double multiplier = totalBet > 0 ? (result.winAmount / totalBet) : 0;

        if (multiplier >= 5)
        {
            waitingForSpecialWin = true;
        }
        else
        {
            waitingForSpecialWin = false;
        }

        yield return new WaitForSeconds(delay);

        if (lastResult == result)
        {
            uiManager.TriggerBigWinPopupEarly(result, () =>
            {
                waitingForSpecialWin = false;
            });
        }
        else
        {
            waitingForSpecialWin = false;
        }
    }

    private void OnWinAnimationComplete()
    {
        if (lastResult != null)
        {
            double totalBet = currentBetAmount * gameConfig.paylineCount;
            double multiplier = totalBet > 0 ? (lastResult.winAmount / totalBet) : 0;

            // Only update UI here if it wasn't already updated in OnReelsStoppedComplete (multiplier < 5)
            if (multiplier >= 5)
            {
                uiManager.OnSpinStopping(lastResult);
            }
        }

        if (isAutoPlaying || isInFreeSpins)
        {
            StartCoroutine(DelayBeforeNextRound());
        }
        else
        {
            ProcessSpinResult();
        }
    }

    private IEnumerator DelayBeforeNextRound()
    {
        yield return new WaitForSeconds(0.5f);

        // Wait for special win popup using the flag and active state
        while (waitingForSpecialWin || uiManager.IsSpecialWinActive)
        {
            yield return null;
        }

        ProcessSpinResult();
    }

    internal void OnSpinResultReceived(SpinResult result)
    {
        lastResult = result;

        // Update free spin counter from server
        if (isInFreeSpins && result.freeGameData != null)
        {
            freeSpinsRemaining = result.freeGameData.freeGameCount;
            uiManager.UpdateFreeSpinCount(freeSpinsRemaining);
        }
    }

    private void ProcessSpinResult()
    {
        playerData = lastResult.playerData;

        uiManager.OnSpinCompleted(lastResult);

        // Extract values before nullifying lastResult
        bool isRoundOver = lastResult.isRoundOver;
        var freeGameData = lastResult.freeGameData;

        lastResult = null;

        if (isAutoPlaying && !isInFreeSpins)
        {
            // Before requesting the next spin, verify the player can still afford it.
            double totalBet = currentBetAmount * gameConfig.paylineCount;
            if (playerData.balance < totalBet)
            {
                currentState = GameState.Idle;
                StopAutoPlay();
                if (popupManager != null) popupManager.ShowInsufficientFundsError();
            }
            else
            {
                currentState = GameState.Idle;
                RequestSpin();
            }
        }
        else if (isInFreeSpins)
        {
            if (isRoundOver || freeSpinsRemaining <= 0)
            {
                EndFreeSpins(0, freeSpinsUsed); // TODO: Use server total round win when available
            }
            else
            {
                currentState = GameState.Idle;
                StartCoroutine(DelayBeforeNextFreeSpin());
            }
        }
        else
        {
            currentState = GameState.Idle;
        }
    }

    #endregion

    #region Auto Play

    internal void StartAutoPlay()
    {
        if (currentState != GameState.Idle) return;

        // Check balance before starting
        double totalBet = currentBetAmount * gameConfig.paylineCount;
        if (playerData.balance < totalBet)
        {
            if (popupManager != null) popupManager.ShowInsufficientFundsError();
            return;
        }

        isAutoPlaying = true;

        uiManager.OnAutoPlayStarted();
        RequestSpin();
    }

    internal void StopAutoPlay()
    {
        isAutoPlaying = false;

        uiManager.OnAutoPlayStopped();
    }

    #endregion

    #region Free Spins

    private void StartFreeSpins(int spins)
    {
        isInFreeSpins = true;
        freeSpinsRemaining = spins;
        freeSpinsUsed = 0;
        waitingForFreeSpinStart = true;

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        uiManager.OnFreeSpinsStarted(spins);

        currentState = GameState.Idle;
    }

    internal void StartFirstFreeSpin()
    {
        waitingForFreeSpinStart = false;

        StartCoroutine(DelayBeforeFirstFreeSpin());
    }

    private IEnumerator DelayBeforeFirstFreeSpin()
    {
        yield return new WaitForSeconds(0.5f);
        RequestSpin();
    }

    private IEnumerator DelayBeforeNextFreeSpin()
    {
        yield return new WaitForSeconds(0.3f);

        // Wait for special win popup if it's still active or pending
        while (waitingForSpecialWin || uiManager.IsSpecialWinActive)
        {
            yield return null;
        }

        RequestSpin();
    }

    private void EndFreeSpins(double totalRoundWin, int totalSpinsUsed)
    {
        isInFreeSpins = false;
        freeSpinsRemaining = 0;

        // Clear sticky wild overlays and stored state
        if (slotView != null)
        {
            slotView.ClearStickyWilds();
        }

        uiManager.OnFreeSpinsEnded(totalRoundWin, totalSpinsUsed);

        currentState = GameState.Idle;
    }

    #endregion

    #region Connection Events

    internal void OnDisconnected()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        currentState = GameState.Idle;
    }

    internal void ExitGame()
    {
        socketManager.CloseSocket();
    }

    #endregion

    #region Helper Methods

    internal bool CanAffordBet()
    {
        double totalBet = currentBetAmount * gameConfig.paylineCount;
        return playerData.balance >= totalBet;
    }

    internal bool IsSpinning()
    {
        return currentState == GameState.Spinning || currentState == GameState.Stopping;
    }

    #endregion
}