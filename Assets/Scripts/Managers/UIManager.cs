using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;

public class UIManager : MonoBehaviour
{
    [Header("Bonus UI")]
    [SerializeField] private WheelBonusPanel wheelBonusPanel;
    [SerializeField] private RectTransform wheelBonusRect;

    [Header("UI References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PopupManager popupManager; 
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private float initializationTimeout = 20f;

    [Header("Backgrounds")]
    [SerializeField] private GameObject normalSpinBackground;
    [SerializeField] private GameObject freeSpinBackground;

    [Header("Bet Controls")]
    [SerializeField] private TMP_Text betAmountText;
    [SerializeField] private Button betPlusButton;
    [SerializeField] private Button betMinusButton;

    [Header("Balance & Win")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text winAmountText;



    [Header("Win Popup Panel")]
    [SerializeField] private GameObject winPopupPanel;
    [SerializeField] private GameObject winRingObject;
    [SerializeField] private ImageAnimation winPopupImageAnimation;
    [SerializeField] private RectTransform winPopupImageRect;
    [SerializeField] private TMP_Text winPopupText;
    [SerializeField] private List<Sprite> bigWinSprites;      // shown at >= 50x
    [SerializeField] private List<Sprite> colossalWinSprites;  // shown at >= 100x

    [Header("Spin Controls")]
    [SerializeField] private Button spinButton;
    [SerializeField] private Button stopButton;

    [Header("Auto Play Panel")]
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Button gameQuitButton;


    [Header("Audio Toggles")]
    [Tooltip("Toggle for background music on/off.")]
    [SerializeField] private Toggle musicToggle;
    [Tooltip("Toggle for all SFX sounds on/off.")]
    [SerializeField] private Toggle sfxToggle;

    [Header("Game Rules Panel")]
    [SerializeField] private GameObject gameRulesPanel;
    [SerializeField] private RectTransform gameRulesPanelRect;
    [SerializeField] private Button gameRulesOpenButton;
    [SerializeField] private Button gameRulesBackButton;


    [Tooltip("Assign exactly 6 page RectTransforms that live inside the panel.")]
    [SerializeField] private RectTransform[] gameRulePages;
    [SerializeField] private float pageSlideWidth = 800f;
    [SerializeField] private GameObject[] rulePageIndicators;

    [Header("Game Rules Dynamic Texts")]
    [SerializeField] private TMP_Text wildMultiplierText;
    [SerializeField] private TMP_Text gloveMultiplierText;
    [SerializeField] private TMP_Text hatMultiplierText;
    [SerializeField] private TMP_Text sunglassesMultiplierText;
    [SerializeField] private TMP_Text shoesMultiplierText;
    [SerializeField] private TMP_Text aceMultiplierText;
    [SerializeField] private TMP_Text kingMultiplierText;
    [SerializeField] private TMP_Text queenMultiplierText;
    [SerializeField] private TMP_Text jackMultiplierText;
    [SerializeField] private TMP_Text tenMultiplierText;
    [SerializeField] private TMP_Text nineMultiplierText;
    [SerializeField] private TMP_Text jackpotMultiplierText;
    [SerializeField] private TMP_Text bonusMultiplierText;
    [Header("Free Spin Count Display - Game Screen")]
    [SerializeField] private GameObject freeSpinCountContainer;
    [SerializeField] private TMP_Text freeSpinCountText;
    [SerializeField] private GameObject lastSpinLeftObject;

  

    [Header("Animation Settings")]
    [SerializeField] private float winCountDuration = 0.25f;
    [SerializeField] private float balanceCountDuration = 1.0f;
    [SerializeField] private float popupAppearY = 555f;
    [SerializeField] private float popupFinalY = 165f;
    [SerializeField] private float popupDropDuration = 0.8f;
    [SerializeField] private int popupBounceCount = 2;
    [SerializeField] private float freeSpinIntroDuration = 2f;

    private Tween balanceTween;
    private Tween winTween;
    private double totalFreeSpinWin = 0;
    private double currentDisplayedBalance = 0;
    private int totalFreeSpinsAwarded = 0;
    private int initialFreeSpins = 0;
    private Coroutine winDisplayCoroutine;
    private int currentRulesPage = 0;
    private bool isPageAnimating;
    private double currentWinDisplayValue = 0;
    private bool isSpecialWinActive = false;
    internal bool IsSpecialWinActive => isSpecialWinActive;
    internal System.Action OnSpecialWinComplete;

    private Vector2 touchStartPos;
    private bool isSwiping;
    private const float swipeThreshold = 100f;

    #region Initialization

    private void Update()
    {
        if (gameRulesPanel != null && gameRulesPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
                isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && isSwiping)
            {
                Vector2 touchEndPos = Input.mousePosition;
                float deltaX = touchEndPos.x - touchStartPos.x;

                if (Mathf.Abs(deltaX) > swipeThreshold)
                {
                    if (deltaX < 0)
                        NextRulesPage();
                    else
                        PrevRulesPage();
                }
                isSwiping = false;
            }
        }
    }

    private void Start()
    {
        SetupButtons();
        SetupSettingsPanel();
        SetupGameRulesPanel();
        InitializeBackgrounds();
        StartCoroutine(LoadingSequence());
    }

    private void InitializeBackgrounds()
    {
        if (normalSpinBackground) normalSpinBackground.SetActive(true);
        if (freeSpinBackground) freeSpinBackground.SetActive(false);
    }

    private void InitializeUI()
    {
        if (spinButton) spinButton.gameObject.SetActive(true);
        if (stopButton) stopButton.gameObject.SetActive(false);
        if (gameRulesPanel) gameRulesPanel.SetActive(false);
        if (winPopupPanel) winPopupPanel.SetActive(false);
        if (winRingObject) winRingObject.SetActive(false);
        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
        if (wheelBonusPanel) wheelBonusPanel.gameObject.SetActive(false);
    }



    #endregion

    #region Loading & Intro Sequence

    private IEnumerator LoadingSequence()
    {
        if (gameScreen) gameScreen.SetActive(true);

        // --- Wait for Initialization ---
        float timer = 0f;
        while (!gameManager.isInitialized && !gameManager.initializationFailed && timer < initializationTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (gameManager.initializationFailed || !gameManager.isInitialized)
        {
            if (gameManager.socketManager != null)
            {
                gameManager.socketManager.SetRaycastBlocker(false);
            }

            if (popupManager != null)
            {
                string errorMsg = gameManager.initializationFailed ? "Game failed to initialize." : "Initialization timed out. Please check your connection.";
                popupManager.ShowErrorPopup("Connection Error", errorMsg, true);
            }
            yield break; // Stop the sequence, don't show the game
        }
        // ------------------------------

        AudioManager.Instance?.PlayGameStart();
        AudioManager.Instance?.PlayBgMusic();
        InitializeUI();
    }



    #endregion

    #region Button Setup

    private void SetupButtons()
    {
        if (betPlusButton)  betPlusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayBetPlus();  gameManager.IncreaseBet(); });
        if (betMinusButton) betMinusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayBetMinus(); gameManager.DecreaseBet(); });
        if (spinButton) spinButton.onClick.AddListener(OnSpinButtonPressed);
        if (stopButton) stopButton.onClick.AddListener(OnStopButtonPressed);

        if (autoPlayButton) autoPlayButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); gameManager.ToggleAutoPlay(); });


        if (gameQuitButton) gameQuitButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); OnExitButtonPressed(); });

    }



    private void SetupSettingsPanel()
    {


        // Audio toggles — restore state from AudioManager then wire callbacks
        if (musicToggle)
        {
            if (AudioManager.Instance != null)
                musicToggle.isOn = AudioManager.Instance.MusicEnabled;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            RefreshToggleBgAlpha(musicToggle);
        }
        if (sfxToggle)
        {
            if (AudioManager.Instance != null)
                sfxToggle.isOn = AudioManager.Instance.SfxEnabled;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
            RefreshToggleBgAlpha(sfxToggle);
        }
    }

    private void SetupGameRulesPanel()
    {
        if (gameRulesOpenButton) gameRulesOpenButton.onClick.AddListener(ShowGameRulesPanel);
        if (gameRulesBackButton) gameRulesBackButton.onClick.AddListener(() => { AudioManager.Instance?.PlayPopupClose(); CloseGameRulesPanel(); });
    }

    #endregion

    #region Game Events

    internal void OnGameInitialized()
    {
        currentWinDisplayValue = 0;
        currentDisplayedBalance = gameManager.playerData.balance;
        if (balanceText) balanceText.text = FormatBalance(currentDisplayedBalance);
        UpdateBetDisplay();
        UpdateGameRulesPaytable(gameManager.gameConfig);
    }

    private void UpdateGameRulesPaytable(GameConfig config)
    {
        if (config == null || config.symbols == null) return;

        foreach (var symbolInfo in config.symbols)
        {
            var mults = (symbolInfo.scatterMultipliers != null && symbolInfo.scatterMultipliers.Count > 0) 
                        ? symbolInfo.scatterMultipliers 
                        : symbolInfo.multipliers;

            if (mults != null && mults.Count > 0)
            {
                List<string> displayLines = new List<string>();
                int currentMatchCount = 5;
                
                for (int i = 0; i < mults.Count; i++)
                {
                    if (mults[i] > 0)
                    {
                        displayLines.Add($"{currentMatchCount}  {mults[i]}x");
                    }
                    currentMatchCount--;
                }
                
                if (displayLines.Count > 0)
                {
                    string textValue = string.Join("\n", displayLines);
                    
                    switch (symbolInfo.id)
                    {
                        case 0: if (wildMultiplierText) wildMultiplierText.text = textValue; break;
                        case 1: if (gloveMultiplierText) gloveMultiplierText.text = textValue; break;
                        case 2: if (hatMultiplierText) hatMultiplierText.text = textValue; break;
                        case 3: if (sunglassesMultiplierText) sunglassesMultiplierText.text = textValue; break;
                        case 4: if (shoesMultiplierText) shoesMultiplierText.text = textValue; break;
                        case 5: if (aceMultiplierText) aceMultiplierText.text = textValue; break;
                        case 6: if (kingMultiplierText) kingMultiplierText.text = textValue; break;
                        case 7: if (queenMultiplierText) queenMultiplierText.text = textValue; break;
                        case 8: if (jackMultiplierText) jackMultiplierText.text = textValue; break;
                        case 9: if (tenMultiplierText) tenMultiplierText.text = textValue; break;
                        case 10: if (nineMultiplierText) nineMultiplierText.text = textValue; break;
                        case 11: if (jackpotMultiplierText) jackpotMultiplierText.text = textValue; break;
                        case 12: if (bonusMultiplierText) bonusMultiplierText.text = textValue; break;  
                    }
                }
            }
        }
    }

    internal void OnSpinStarted()
    {
        AudioManager.Instance?.PlaySpinStart();

        if (spinButton) spinButton.gameObject.SetActive(false);
        if (stopButton) stopButton.gameObject.SetActive(true);
        
        if (gameManager.isInFreeSpins || gameManager.isAutoPlaying)
        {
            if (stopButton) stopButton.interactable = false;
        }
        else
        {
            if (stopButton) stopButton.interactable = true;
        }

        if (autoPlayButton) autoPlayButton.interactable = false;

        AudioManager.Instance?.StopWinPopupBg();

        if (winDisplayCoroutine != null)
        {
            StopCoroutine(winDisplayCoroutine);
            winDisplayCoroutine = null;
        }
        if (winPopupPanel)
        {
            winPopupPanel.SetActive(false);
            if (winPopupImageAnimation) winPopupImageAnimation.StopAnimation();
        }
        if (winRingObject) winRingObject.SetActive(false);
        isSpecialWinActive = false;

        // Immediately deduct the total bet from the displayed balance when spin is pressed
        if (!gameManager.isInFreeSpins)
        {
            currentDisplayedBalance -= gameManager.TotalBetAmount;
            if (balanceText) balanceText.text = FormatBalance(currentDisplayedBalance);
        }
    }

    private bool earlyBigWinPopupTriggered = false;

    internal void TriggerBigWinPopupEarly(SpinResult result, System.Action onComplete = null)
    {
        double totalBetAmount = gameManager.TotalBetAmount;
        double winAmount = result.winAmount;
        double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;

        if (multiplier >= 50)
        {
            earlyBigWinPopupTriggered = true;
            if (winDisplayCoroutine != null) StopCoroutine(winDisplayCoroutine);
            winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(result, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    internal void OnSpinStopping(SpinResult result)
    {
        UpdateBalanceDisplay(result.playerData.balance);

        bool isFreeSpin = gameManager.isInFreeSpins && result.freeGameData != null;

        // In free spins: animate to totalRoundWin (cumulative).
        // In normal spins: animate from 0 to winAmount.
        double targetWin = isFreeSpin ? result.freeGameData.totalRoundWin : result.winAmount;
        double startVal  = isFreeSpin ? currentWinDisplayValue : 0;

        if (targetWin > startVal)
        {
            // Animate win text counting up to the target
            if (winTween != null) winTween.Kill();
            double animFrom = startVal;
            winTween = DOTween.To(
                () => animFrom,
                x =>
                {
                    animFrom = x;
                    currentWinDisplayValue = x;
                    if (winAmountText) winAmountText.text = System.Math.Round(x, 3).ToString("0.###");
                },
                targetWin,
                winCountDuration
            ).SetEase(Ease.Linear);

            double multiplier = gameManager.TotalBetAmount > 0 ? (result.winAmount / gameManager.TotalBetAmount) : 0;

            // Only show the popup for Big Win (>= 50x) or Colossal Win (>= 100x)
            if (multiplier >= 50 && !earlyBigWinPopupTriggered)
            {
                ShowWinDisplay(result);
            }
            earlyBigWinPopupTriggered = false;
        }
        else if (!isFreeSpin && result.winAmount <= 0)
        {
            // Normal spin with no win — instantly reset win display to 0
            UpdateWinDisplay(0);
            earlyBigWinPopupTriggered = false;
        }
        // else: free spin with same totalRoundWin (no new win this spin) — leave display unchanged
    }

    internal void OnSpinCompleted(SpinResult result)
    {
        if (isSpecialWinActive || (gameManager != null && gameManager.IsSpecialWinPending)) return;

        if (gameManager.isAutoPlaying)
        {
            if (spinButton) spinButton.gameObject.SetActive(false);
            if (stopButton) stopButton.gameObject.SetActive(true);
            if (stopButton) stopButton.interactable = false; // No stopping autoplay via spin/stop button
        }
        else if (gameManager.isInFreeSpins)
        {
            if (spinButton) spinButton.gameObject.SetActive(false);
            if (stopButton) stopButton.gameObject.SetActive(false);
        }
        else
        {
            if (spinButton) spinButton.gameObject.SetActive(true);
            if (stopButton) stopButton.gameObject.SetActive(false);

            SetBetControlsEnabled(true);
            if (autoPlayButton) autoPlayButton.interactable = true;
            if (spinButton) spinButton.interactable = true;
        }
    }

    internal void DisableControlsDuringWinAnimation()
    {
        SetBetControlsEnabled(false);
        if (spinButton)
        {
            spinButton.gameObject.SetActive(true);
            spinButton.interactable = false;
        }
        if (stopButton)
        {
            stopButton.gameObject.SetActive(true);
            stopButton.interactable = false;
        }
        
        if (gameManager != null && gameManager.lastResult != null)
        {
            double winAmount = gameManager.lastResult.winAmount;
            double multiplier = gameManager.TotalBetAmount > 0 ? (winAmount / gameManager.TotalBetAmount) : 0;
            
            if (multiplier >= 50)
            {
                if (winRingObject) winRingObject.SetActive(true);
            }
        }
    }

    internal void EnableControlsAfterWinAnimation()
    {
        if (isSpecialWinActive || (gameManager != null && gameManager.IsSpecialWinPending)) return;

        if (gameManager.isAutoPlaying)
        {
            if (spinButton) spinButton.gameObject.SetActive(false);
            if (stopButton)
            {
                stopButton.gameObject.SetActive(true);
                stopButton.interactable = false;
            }
        }
        else if (gameManager.isInFreeSpins)
        {
            if (spinButton) spinButton.gameObject.SetActive(false);
            if (stopButton)
            {
                stopButton.gameObject.SetActive(false);
                stopButton.interactable = false;
            }
        }
        else
        {
            SetBetControlsEnabled(true);
            if (spinButton)
            {
                spinButton.gameObject.SetActive(true);
                spinButton.interactable = true;
            }
            if (autoPlayButton) autoPlayButton.interactable = true;
            if (stopButton)
            {
                stopButton.gameObject.SetActive(false);
                stopButton.interactable = false;
            }
        }
    }

    private void ShowWinDisplay(SpinResult result)
    {
        if (winDisplayCoroutine != null) StopCoroutine(winDisplayCoroutine);
        winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(result));
    }

    private IEnumerator ShowWinDisplayCoroutine(SpinResult result, System.Action onComplete = null)
    {
        double winAmount = result.winAmount;
        double totalBetAmount = gameManager.TotalBetAmount;
        double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;

        double startVal = gameManager.isInFreeSpins ? currentWinDisplayValue : 0;
        double endVal = winAmount;
        double popupWinAmount = winAmount;

        isSpecialWinActive = true;
        DisableControlsDuringWinAnimation();

        AudioManager.Instance?.PlayWinOpeningJingle(multiplier);
        AudioManager.Instance?.PlayWinPopupBg(multiplier);


        List<Sprite> selectedSprites = null;
        float popupTime = 2f; // Both tiers use 2 second popup

        if (multiplier >= 100)
        {
            // Colossal Win
            selectedSprites = colossalWinSprites;
        }
        else
        {
            // Big Win (>= 50x)
            selectedSprites = bigWinSprites;
        }

        if (winPopupImageAnimation)
        {
            winPopupImageAnimation.textureArray = selectedSprites;
        }

        if (winPopupPanel) winPopupPanel.SetActive(true);

        if (winPopupImageAnimation)
        {
            winPopupImageAnimation.StartAnimation();
        }

        float animDuration = popupTime - 1f;

        if (winPopupImageRect)
        {
            winPopupImageRect.localScale = Vector3.zero;
            winPopupImageRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).OnComplete(() => {
                winPopupImageRect.DOScale(new Vector3(1.2f, 1.2f, 1.2f), animDuration - 0.5f).SetEase(Ease.Linear);
            });
        }

        if (winPopupText)
        {
            winPopupText.text = "0";
            double currentAnimVal = startVal;
            DOTween.To(() => currentAnimVal, x => {
                currentAnimVal = x;

                double range = endVal - startVal;
                double progress = range > 0 ? Math.Max(0, Math.Min(1, (currentAnimVal - startVal) / range)) : 1.0;

                double currentPopupHit = Math.Round(progress * popupWinAmount, 2);
                winPopupText.text = currentPopupHit.ToString();

                double displayVal = Math.Round(currentAnimVal, 2);
                if (winAmountText) winAmountText.text = displayVal.ToString();

                currentWinDisplayValue = displayVal;
            }, endVal, animDuration).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(popupTime);

        AudioManager.Instance?.StopWinPopupBg();

        if (winPopupPanel) winPopupPanel.SetActive(false);
        if (winPopupImageAnimation) winPopupImageAnimation.StopAnimation();
        if (winRingObject) winRingObject.SetActive(false);

        isSpecialWinActive = false;
        EnableControlsAfterWinAnimation();
        OnSpinCompleted(null);

        onComplete?.Invoke();
        OnSpecialWinComplete?.Invoke();

        winDisplayCoroutine = null;
    }

    #endregion

    #region Spin Button

    private void OnSpinButtonPressed()
    {
        if (gameManager.isAutoPlaying) return;
        gameManager.RequestSpin();
    }

    private void OnStopButtonPressed()
    {
        gameManager.RequestStopSpin();
    }

    #endregion

    #region Bet Controls

    internal void UpdateBetDisplay()
    {
        if (gameManager.gameConfig == null) return;

        double totalBetAmount = gameManager.TotalBetAmount;

        if (betAmountText)
            betAmountText.text = totalBetAmount.ToString();
        UpdateBetButtonStates();
    }

    private void UpdateBetButtonStates()
    {
        if (betMinusButton) betMinusButton.interactable = true;
        if (betPlusButton) betPlusButton.interactable = true;
    }

   


    #endregion

    #region Auto Play

    internal void OnAutoPlayStarted()
    {
        
        if (spinButton) spinButton.gameObject.SetActive(false);
        if (stopButton) stopButton.gameObject.SetActive(true);
        SetBetControlsEnabled(false);
    }

    internal void OnAutoPlayStopped()
    {
        // If game is not spinning and no round is in progress, restore controls
        bool isRoundActive = gameManager.IsSpinning() || gameManager.lastResult != null;

        if (!isRoundActive && !gameManager.isInFreeSpins)
        {
            if (spinButton) spinButton.gameObject.SetActive(true);
            if (stopButton) stopButton.gameObject.SetActive(false);
            if (spinButton) spinButton.interactable = true;

            SetBetControlsEnabled(true);
            if (autoPlayButton) autoPlayButton.interactable = true;
        }
        else if (isRoundActive)
        {
            if (stopButton) stopButton.interactable = false;
        }
    }

    #endregion

    #region Settings Toggles

    private void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButton();
        AudioManager.Instance?.SetMusicEnabled(isOn);
        RefreshToggleBgAlpha(musicToggle);
    }

    private void OnSfxToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButton();
        AudioManager.Instance?.SetSfxEnabled(isOn);
        RefreshToggleBgAlpha(sfxToggle);
    }



    // Reads the background Image directly from Toggle.targetGraphic.
    // Sets alpha to 0 when the toggle is ON so the checkmark is not obscured,
    // and restores full alpha when the toggle is OFF.
    private static void RefreshToggleBgAlpha(Toggle toggle)
    {
        if (toggle == null) return;
        Image bgImage = toggle.targetGraphic as Image;
        if (bgImage == null) return;
        Color c = bgImage.color;
        c.a = toggle.isOn ? 0f : 1f;
        bgImage.color = c;
    }

    #endregion

    #region Settings Panel


    private void ShowGameRulesPanel()
    {
        if (gameRulesPanel == null) return;

        currentRulesPage = 0;
        isPageAnimating = false;

        if (gameRulePages != null)
        {
            for (int i = 0; i < gameRulePages.Length; i++)
            {
                if (gameRulePages[i] == null) continue;
                gameRulePages[i].gameObject.SetActive(true);
                gameRulePages[i].anchoredPosition = new Vector2(i * pageSlideWidth, 0f);
            }
        }

        UpdateRulePageIndicators(currentRulesPage);

        gameRulesPanel.SetActive(true);

        if (gameRulesPanelRect)
        {
            gameRulesPanelRect.localScale = Vector3.zero;
            gameRulesPanelRect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void CloseGameRulesPanel()
    {
        if (gameRulesPanel == null || !gameRulesPanel.activeSelf) return;

        if (gameRulesPanelRect)
        {
            gameRulesPanelRect.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    gameRulesPanel.SetActive(false);
                    gameRulesPanelRect.localScale = Vector3.one;
                });
        }
        else
        {
            gameRulesPanel.SetActive(false);
        }
    }

    private void NextRulesPage()
    {
        if (isPageAnimating || gameRulePages == null || gameRulePages.Length == 0) return;
        int next = (currentRulesPage + 1) % gameRulePages.Length;
        SlideToPage(currentRulesPage, next, slideLeft: true);
    }

    private void PrevRulesPage()
    {
        if (isPageAnimating || gameRulePages == null || gameRulePages.Length == 0) return;
        int prev = (currentRulesPage - 1 + gameRulePages.Length) % gameRulePages.Length;
        SlideToPage(currentRulesPage, prev, slideLeft: false);
    }

    private void SlideToPage(int fromIndex, int toIndex, bool slideLeft)
    {
        if (gameRulePages == null) return;
        if (fromIndex < 0 || fromIndex >= gameRulePages.Length) return;
        if (toIndex < 0 || toIndex >= gameRulePages.Length) return;

        RectTransform fromPage = gameRulePages[fromIndex];
        RectTransform toPage = gameRulePages[toIndex];
        if (fromPage == null || toPage == null) return;

        AudioManager.Instance?.PlayPageSwipe();
        isPageAnimating = true;

        float direction = slideLeft ? 1f : -1f;

        toPage.anchoredPosition = new Vector2(direction * pageSlideWidth, 0f);
        toPage.gameObject.SetActive(true);
        fromPage.anchoredPosition = new Vector2(0f, 0f);

        float slideDuration = 0.35f;

        fromPage.DOAnchorPosX(-direction * pageSlideWidth, slideDuration).SetEase(Ease.InOutCubic);

        toPage.DOAnchorPosX(0f, slideDuration)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                fromPage.anchoredPosition = new Vector2(direction * pageSlideWidth, 0f);
                currentRulesPage = toIndex;
                isPageAnimating = false;
                UpdateRulePageIndicators(currentRulesPage);
            });
    }

    private void UpdateRulePageIndicators(int activeIndex)
    {
        if (rulePageIndicators == null || rulePageIndicators.Length == 0) return;
        for (int i = 0; i < rulePageIndicators.Length; i++)
        {
            if (rulePageIndicators[i] == null) continue;
            // Enable the first child for the active index, disable for others
            if (rulePageIndicators[i].transform.childCount > 0)
            {
                rulePageIndicators[i].transform.GetChild(0).gameObject.SetActive(i == activeIndex);
            }
        }
    }

    #endregion



    #region Free Spins

    internal void OnFreeSpinsStarted(int spinsAwarded)
    {
        initialFreeSpins = spinsAwarded;
        totalFreeSpinsAwarded = spinsAwarded;

        // Kill any running win tween (e.g. from wheel bonus win amount) before resetting
        if (winTween != null) { winTween.Kill(); winTween = null; }
        currentWinDisplayValue = 0;
        UpdateWinDisplay(0);

        if (normalSpinBackground) normalSpinBackground.SetActive(false);
        if (freeSpinBackground) freeSpinBackground.SetActive(true);
        
        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(true);
        UpdateFreeSpinCount(spinsAwarded);

        if (spinButton) spinButton.gameObject.SetActive(false);
        if (stopButton) stopButton.gameObject.SetActive(false);
        SetBetControlsEnabled(false);
    }

    internal void OnFreeSpinsEnded(double serverTotalRoundWin, int serverTotalSpinsUsed)
    {
        if (normalSpinBackground) normalSpinBackground.SetActive(true);
        if (freeSpinBackground) freeSpinBackground.SetActive(false);

        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);

        if (spinButton) spinButton.gameObject.SetActive(true);
        if (stopButton) stopButton.gameObject.SetActive(false);
        SetBetControlsEnabled(true);
    }

    internal void UpdateFreeSpinCount(int remainingSpins)
    {
        if (remainingSpins == 1)
        {
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(true);
        }
        else if (remainingSpins == 0)
        {
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
        }
        else
        {
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(true);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
            if (freeSpinCountText) freeSpinCountText.text = remainingSpins.ToString();
        }
    }

    #endregion



    #region Cleanup

    private void OnDestroy()
    {
        if (balanceTween != null) balanceTween.Kill();
        if (winTween != null) winTween.Kill();
        DOTween.KillAll();
    }

    #endregion

    #region Connection Popup Management


    private void OnExitButtonPressed()
    {
        if (gameManager != null) gameManager.ExitGame();
    }

    #endregion
    internal void UpdateBalanceDisplay(double newBalance)
    {
        if (balanceTween != null) balanceTween.Kill();

        double fromBalance = currentDisplayedBalance;

        // If the server balance matches what we already show (no win, no difference),
        // just sync quietly without animation.
        if (System.Math.Abs(newBalance - fromBalance) < 0.0001)
        {
            currentDisplayedBalance = newBalance;
            if (balanceText) balanceText.text = FormatBalance(newBalance);
            return;
        }

        // Animate from the current displayed value to the server-authoritative balance.
        double animFrom = fromBalance;
        balanceTween = DOTween.To(
            () => animFrom,
            x =>
            {
                animFrom = x;
                currentDisplayedBalance = x;
                if (balanceText) balanceText.text = FormatBalance(x);
            },
            newBalance,
            balanceCountDuration
        ).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            currentDisplayedBalance = newBalance;
            if (balanceText) balanceText.text = FormatBalance(newBalance);
        });
    }

    #region Wheel Bonus

    internal void ShowWheelBonus(WheelBonusConfig config, ServerWheelBonusResult result, Action<ServerWheelBonusResult> onComplete)
    {
        if (wheelBonusPanel == null)
        {
            onComplete?.Invoke(result);
            return;
        }

        wheelBonusPanel.gameObject.SetActive(true);
        wheelBonusRect.localScale = Vector3.one;
        
        if (wheelBonusRect != null)
        {
            wheelBonusRect.localScale = Vector3.zero;
            wheelBonusRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }

        wheelBonusPanel.Setup(config, result, (finalResult) =>
        {
            if (wheelBonusRect != null)
            {
                wheelBonusRect.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    wheelBonusPanel.gameObject.SetActive(false);
                    onComplete?.Invoke(finalResult);
                });
            }
            else
            {
                wheelBonusPanel.gameObject.SetActive(false);
                onComplete?.Invoke(finalResult);
            }
        });

        wheelBonusPanel.StartBonus();
    }

    #endregion

    private void UpdateWinDisplay(double amount)
    {
        currentWinDisplayValue = amount;
        if (winAmountText)
            winAmountText.text = amount.ToString();
    }

    internal void AnimateWinDisplay(double targetAmount)
    {
        if (winTween != null) winTween.Kill();

        double startVal = currentWinDisplayValue;
        winTween = DOTween.To(() => startVal, x =>
        {
            startVal = x;
            UpdateWinDisplay(x);
        }, targetAmount, winCountDuration).SetEase(Ease.OutQuad);
    }

    private void SetBetControlsEnabled(bool enabled)
    {
        if (betPlusButton) betPlusButton.interactable = enabled;
        if (betMinusButton) betMinusButton.interactable = enabled;
        if (autoPlayButton) autoPlayButton.interactable = enabled;
    }

    /// <summary>
    /// Formats a balance value: shows up to 3 decimal places,
    /// but drops trailing zeros and the decimal point for whole numbers.
    /// Examples: 63706.116 → "63706.116", 1000.0 → "1000", 0.025 → "0.025"
    /// </summary>
    private static string FormatBalance(double value)
    {
        return System.Math.Round(value, 3).ToString("0.###");
    }
}
