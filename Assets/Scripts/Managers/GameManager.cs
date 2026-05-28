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

    private GameState currentState;

    internal int currentBetIndex;
    internal double currentBetAmount;
    internal double TotalBetAmount => currentBetAmount * (gameConfig?.paylineCount ?? 1);

    internal bool isAutoPlaying;

    internal bool isInFreeSpins;
    private int freeSpinsRemaining;
    private int freeSpinsUsed;
    private bool waitingForFreeSpinStart;

    internal bool isInitialized;
    internal bool initializationFailed;

    private Coroutine spinCoroutine;
    private bool stopRequested;
    private bool waitingForSpecialWin;
    internal bool IsSpecialWinPending => waitingForSpecialWin;

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

        if (!isInFreeSpins && playerData.balance < TotalBetAmount)
        {
            if (popupManager != null)
            {
                popupManager.ShowInsufficientFundsError();
            }
            return;
        }

        StartSpin();
    }

    internal void RequestStopSpin()
    {
        if (currentState == GameState.Spinning && !isInFreeSpins)
        {
            stopRequested = true;
        }
    }

    internal void ToggleAutoPlay()
    {
        if (isAutoPlaying)
            StopAutoPlay();
        else
            StartAutoPlay();
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
        if (lastResult.wheelBonusTriggered)
        {
            StartWheelBonus(lastResult.wheelBonusResult);
            return;
        }

        if (lastResult.winAmount > 0 && lastResult.winLines != null && lastResult.winLines.Count > 0)
        {
            double multiplier = TotalBetAmount > 0 ? (lastResult.winAmount / TotalBetAmount) : 0;

            currentState = GameState.Idle;

            if (multiplier < 50)
            {
                // Normal win: update balance/win display, then enable spin immediately
                uiManager.OnSpinStopping(lastResult);
                // Enable spin right away — win animation plays in background
                uiManager.EnableControlsAfterWinAnimation();
            }
            else
            {
                // Big Win / Colossal Win (>= 50x): keep controls disabled until popup is dismissed
                uiManager.DisableControlsDuringWinAnimation();
            }

            // Fire win animation in background (non-blocking for spin button)
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
        double multiplier = TotalBetAmount > 0 ? (result.winAmount / TotalBetAmount) : 0;

        if (multiplier >= 50)
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
        // If lastResult is null, the player already pressed spin again (bypassed win animation).
        // Nothing to do here — the new spin already called ProcessSpinResult via StartSpin.
        if (lastResult == null) return;

        // For big wins (>= 5x), the popup handles its own completion via ShowWinDisplayCoroutine.
        // For normal wins, controls are already enabled — just handle auto/freespin chains.
        if (isAutoPlaying || isInFreeSpins)
        {
            StartCoroutine(DelayBeforeNextRound());
        }
        else
        {
            // Normal play: process result (spin button already enabled when reels stopped)
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
        if (lastResult == null) return; // Already processed (e.g., player bypassed win animation)

        playerData = lastResult.playerData;

        uiManager.OnSpinCompleted(lastResult);

        // Extract values before nullifying lastResult
        bool isRoundOver = lastResult.isRoundOver;
        var freeGameData = lastResult.freeGameData;

        lastResult = null;

        if (isAutoPlaying && !isInFreeSpins)
        {
            if (playerData.balance < TotalBetAmount)
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

        if (playerData.balance < TotalBetAmount)
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

    #region Wheel Bonus

    private void StartWheelBonus(ServerWheelBonusResult result)
    {
        if (uiManager != null)
        {
            uiManager.ShowWheelBonus(gameConfig.wheelBonus, result, OnWheelBonusComplete);
        }
        else
        {
            OnWheelBonusComplete(result);
        }
    }

    private void OnWheelBonusComplete(ServerWheelBonusResult result)
    {
        // 1. Update balance and win display using authoritative server data
        if (lastResult != null)
        {
            uiManager.UpdateBalanceDisplay(lastResult.playerData.balance);
            uiManager.AnimateWinDisplay(lastResult.winAmount);
        }
        else if (result.creditAward > 0)
        {
            // Fallback if lastResult is somehow missing (should not happen in normal flow)
            playerData.balance += result.creditAward;
            uiManager.UpdateBalanceDisplay(playerData.balance);
            uiManager.AnimateWinDisplay(result.creditAward);
        }

        // 2. Trigger free spins if awarded
        if (result.result.type == "freeGames")
        {
            StartFreeSpins(result.result.count ?? 0);
            StartFirstFreeSpin();
        }
        else
        {
            // If it was just credits, we return to the normal flow
            if (isAutoPlaying)
            {
                StartCoroutine(DelayBeforeNextRound());
            }
            else
            {
                currentState = GameState.Idle;
                uiManager.OnSpinCompleted(lastResult);
            }
        }
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
        return playerData.balance >= TotalBetAmount;
    }

    internal bool IsSpinning()
    {
        return currentState == GameState.Spinning || currentState == GameState.Stopping;
    }

    #endregion
}