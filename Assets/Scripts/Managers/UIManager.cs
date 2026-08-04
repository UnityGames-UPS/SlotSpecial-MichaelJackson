using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;

public class UIManager : MonoBehaviour
{
    [SerializeField] private AudioController audioController;

    [Header("Bonus UI")]
    [SerializeField] private WheelBonusPanel wheelBonusPanel;
    [SerializeField] private RectTransform wheelBonusRect;
    [SerializeField] private GameObject WheelStartAnimation;

    [Header("UI References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private float initializationTimeout = 20f;

    [Header("Backgrounds")]
    [SerializeField] private GameObject normalSpinBackground;
    [SerializeField] private GameObject freeSpinBackground;
    //[SerializeField] private Image BlurrBg;
    [SerializeField] private Sprite NormalBG;
    [SerializeField] private Image SlotBg;
    [SerializeField] private Sprite NormalSpinSlotBG;
    [SerializeField] private Sprite FreeSpinSlotBG;
    [SerializeField] private Sprite beatItBG;
    [SerializeField] private Sprite smoothCriminalBG;
    [SerializeField] private Image TitleImage;
    [SerializeField] private Sprite defaultTitleImage;
    [SerializeField] private Sprite beatItTitle;
    [SerializeField] private Sprite smoothCriminalTitle;

    [Header("Free Spin Intro Panel")]
    [SerializeField] private Image IntroTitleImage;
    [SerializeField] private Sprite beatItIntroTitle;
    [SerializeField] private Sprite smoothCriminalIntroTitle;
    [SerializeField] private Button buttonStartFreeSpin;

    [Header("Bet Controls")]
    [SerializeField] private TMP_Text betAmountText;
    [SerializeField] private Button betPlusButton;
    [SerializeField] private Button betMinusButton;

    [Header("Balance & Win")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text winAmountText;



    [Header("Win Popup Panel")]
    [SerializeField] private GameObject winPopupPanel;
    [SerializeField] private SpineAnimController BGAnimation;
    [SerializeField] private Image winPopupTitleImage;
    [SerializeField] private GameObject winTitleBG;
    [SerializeField] private ImageAnimation FirstFireWorkImageAnimation;
    [SerializeField] private ImageAnimation SecondFireWorkImageAnimation;
    [SerializeField] private SpineAnimController CoinDiamondAnimation;
    //[SerializeField] private GameObject winRingObject;
    [SerializeField] private RectTransform winStatueImageRect;
    [SerializeField] private TMP_Text winPopupText;
    [SerializeField] private Sprite bigWinSprite;      // shown at >= 50x
    [SerializeField] private Sprite colossalWinSprite;  // shown at >= 100x

    [Header("Simple Win Popup Panel")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private CanvasGroup winPanelBG;
    [SerializeField] private TMP_Text winText;

    [Header("Spin Controls")]
    [SerializeField] private Button spinButton;
    [SerializeField] private Button stopButton;

    [Header("Auto Play Panel")]
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Transform autoPlayRotateObject;

    [Header("Game Quit")]
    [SerializeField] private Button gameQuitButton;
    [SerializeField] private GameObject Quit_Panel;
    [SerializeField] private RectTransform quitRect;
    [SerializeField] private Button Yes_Button;
    [SerializeField] private Button No_Button;


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
    private Tween autoPlayRotationTween;
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

    private void Awake()
    {
        gameManager?.socketManager?.JSManager?.RegisterVisibilityListener(gameObject.name);
    }

    public void OnFocusChanged(string value)
    {
        bool focused = value == "1";
        Debug.Log("UNITY FOCUS CHANGED: " + value + " (focused: " + focused + ")");
        audioController?.SetMuteAll(!focused);
        gameManager?.socketManager?.HandleFocusChange(focused);
    }

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

                // FIX: orientation-aware swipe detection for rules/info page navigation.
                // In landscape mode, the pages scroll horizontally (check X-axis).
                // In portrait mode (UIWrapper rotated -90 degrees), a visual horizontal swipe 
                // becomes a vertical swipe in screen coordinates (check Y-axis).
                if (OrientationChange.IsLandscapeOrientation)
                {
                    // Landscape: standard horizontal swipe (left/right on X-axis)
                    float deltaX = touchEndPos.x - touchStartPos.x;
                    if (Mathf.Abs(deltaX) > swipeThreshold)
                    {
                        if (deltaX < 0)
                            NextRulesPage();   // Swipe left -> next page
                        else
                            PrevRulesPage();   // Swipe right -> prev page
                    }
                }
                else
                {
                    // Portrait: UI is rotated -90 degrees. A visual horizontal swipe becomes 
                    // a vertical swipe in screen coordinates. Check Y-axis instead.
                    // Swipe up (positive deltaY) -> next page, swipe down (negative deltaY) -> prev page
                    float deltaY = touchEndPos.y - touchStartPos.y;
                    if (Mathf.Abs(deltaY) > swipeThreshold)
                    {
                        if (deltaY > 0)
                            NextRulesPage();   // Swipe up -> next page
                        else
                            PrevRulesPage();   // Swipe down -> prev page
                    }
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
        //StartCoroutine(PlayBigWinVisuals(35.50f, 0f, true)); // Preload the big win visuals to avoid first-time lag
    }

    private void InitializeBackgrounds()
    {
        if (normalSpinBackground)
        {
            normalSpinBackground.SetActive(true);
            normalSpinBackground.GetComponentInChildren<ImageAnimation>().StartAnimation();
        }
        if (freeSpinBackground) freeSpinBackground.SetActive(false);
    }

    private void InitializeUI()
    {
        if (spinButton) spinButton.gameObject.SetActive(true);
        if (stopButton) stopButton.gameObject.SetActive(false);
        if (gameRulesPanel) gameRulesPanel.SetActive(false);
        if (winPopupPanel) winPopupPanel.SetActive(false);
        //if (winRingObject) winRingObject.SetActive(false);
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

        InitializeUI();
    }



    #endregion

    #region Button Setup

    private void SetupButtons()
    {
        if (betPlusButton) betPlusButton.onClick.AddListener(() => { audioController.PlayUIButton(); gameManager.IncreaseBet(); });
        if (betMinusButton) betMinusButton.onClick.AddListener(() => { audioController.PlayUIButton(); gameManager.DecreaseBet(); });
        if (spinButton) spinButton.onClick.AddListener(OnSpinButtonPressed);
        if (stopButton) stopButton.onClick.AddListener(OnStopButtonPressed);

        if (autoPlayButton) autoPlayButton.onClick.AddListener(() => { audioController.PlayUIButton(); gameManager.ToggleAutoPlay(); });

        if (gameQuitButton) gameQuitButton.onClick.AddListener(() => { audioController.PlayUIButton(); ShowQuitPanel(); });
        if (Yes_Button) Yes_Button.onClick.AddListener(() => { audioController.PlayUIButton(); OnExitButtonPressed(); });
        if (No_Button) No_Button.onClick.AddListener(() => { audioController.PlayUIButton(); CloseQuitPanel(); });

        if (buttonStartFreeSpin) buttonStartFreeSpin.onClick.AddListener(() => { FreeSpinStartButtonPressed(); });

    }



    private void SetupSettingsPanel()
    {


        // Audio toggles — restore state from AudioManager then wire callbacks
        if (musicToggle)
        {
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            RefreshToggleBgAlpha(musicToggle);
        }
        if (sfxToggle)
        {
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
            RefreshToggleBgAlpha(sfxToggle);
        }
    }

    private void SetupGameRulesPanel()
    {
        if (gameRulesOpenButton) gameRulesOpenButton.onClick.AddListener(ShowGameRulesPanel);
        if (gameRulesBackButton) gameRulesBackButton.onClick.AddListener(() => { CloseGameRulesPanel(); });
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
            else
            {
                switch (symbolInfo.id)
                {
                    case 0: if (wildMultiplierText) wildMultiplierText.text = ""; break;
                    case 1: if (gloveMultiplierText) gloveMultiplierText.text = ""; break;
                    case 2: if (hatMultiplierText) hatMultiplierText.text = ""; break;
                    case 3: if (sunglassesMultiplierText) sunglassesMultiplierText.text = ""; break;
                    case 4: if (shoesMultiplierText) shoesMultiplierText.text = ""; break;
                    case 5: if (aceMultiplierText) aceMultiplierText.text = ""; break;
                    case 6: if (kingMultiplierText) kingMultiplierText.text = ""; break;
                    case 7: if (queenMultiplierText) queenMultiplierText.text = ""; break;
                    case 8: if (jackMultiplierText) jackMultiplierText.text = ""; break;
                    case 9: if (tenMultiplierText) tenMultiplierText.text = ""; break;
                    case 10: if (nineMultiplierText) nineMultiplierText.text = ""; break;
                    case 11: if (jackpotMultiplierText) jackpotMultiplierText.text = ""; break;
                    case 12: if (bonusMultiplierText) bonusMultiplierText.text = ""; break;
                }
            }
        }
    }

    internal void OnSpinStarted()
    {

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

        if (autoPlayButton) autoPlayButton.interactable = gameManager.isAutoPlaying;

        if (winDisplayCoroutine != null)
        {
            StopCoroutine(winDisplayCoroutine);
            winDisplayCoroutine = null;
        }
        if (winPopupPanel)
        {
            winPopupPanel.SetActive(false);
            winPopupTitleImage.gameObject.SetActive(false);
            winPanel.SetActive(false);
            //if (winPopupImageAnimation) winPopupImageAnimation.StopAnimation();
        }
        //if (winRingObject) winRingObject.SetActive(false);
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

        // Big/colossal win popups never fire mid free-spins — the round-total check
        // happens once, after free spins end (see ShowPostRoundWinSequence).
        if (multiplier >= 50 && !gameManager.isInFreeSpins)
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
        double startVal = isFreeSpin ? currentWinDisplayValue : 0;

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
                    if (winAmountText) winAmountText.text = System.Math.Round(x, 3).ToString("F3");
                },
                targetWin,
                winCountDuration
            ).SetEase(Ease.Linear);

            double multiplier = gameManager.TotalBetAmount > 0 ? (result.winAmount / gameManager.TotalBetAmount) : 0;

            // Only show the popup for Big Win (>= 50x) or Colossal Win (>= 100x), and never
            // mid free-spins — the round-total check happens once, after free spins end.
            if (multiplier >= 30 && !earlyBigWinPopupTriggered && !gameManager.isInFreeSpins)
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
            if (stopButton) stopButton.gameObject.SetActive(true);
            if (stopButton) stopButton.interactable = false;
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
                //if (winRingObject) winRingObject.SetActive(true);
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

    private IEnumerator WinPanelRoutine(double winAmount, System.Action onBackgroundOpaque = null)
    {
        // if (winPanel == null || winPanelBG == null || winText == null)
        // {
        //     onBackgroundOpaque?.Invoke();
        //     yield break;
        // }
        // audioController.PlayNormalWin();
        // winText.text = System.Math.Round(winAmount, 3).ToString("0.###");
        winPanelBG.alpha = 0f;
        winPanel.SetActive(true);

        // yield return new WaitForSeconds(1.5f);

        winPanelBG.DOFade(1f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // Background has just finished fading to fully opaque — safe moment for a
        // caller to swap out whatever's behind it without the change being visible.
        onBackgroundOpaque?.Invoke();

        yield return new WaitForSeconds(0.5f);
        winPanel.SetActive(false);
    }

    private void ShowWinDisplay(SpinResult result)
    {
        if (winDisplayCoroutine != null) StopCoroutine(winDisplayCoroutine);
        winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(result));
    }

    private IEnumerator ShowWinDisplayCoroutine(SpinResult result, System.Action onComplete = null)
    {
        double winAmount = result.winAmount;
        double startVal = gameManager.isInFreeSpins ? currentWinDisplayValue : 0;

        isSpecialWinActive = true;
        DisableControlsDuringWinAnimation();

        yield return StartCoroutine(PlayBigWinVisuals(winAmount, startVal));

        isSpecialWinActive = false;
        EnableControlsAfterWinAnimation();
        OnSpinCompleted(null);

        onComplete?.Invoke();
        OnSpecialWinComplete?.Invoke();

        winDisplayCoroutine = null;
    }

    /// <summary>
    /// FIX: Waits for an ImageAnimation to reach FINISHED, but with a timeout safety net.
    /// Previously we did `yield return new WaitUntil(() => anim.currentAnimationState == FINISHED)`
    /// directly. If that ImageAnimation's state ever got stuck (see the StartAnimation()
    /// fix in ImageAnimation.cs for why that could happen), this WaitUntil would block
    /// forever — which in turn kept isSpecialWinActive stuck true and permanently
    /// disabled the spin/bet/autoplay buttons. This wrapper guarantees we always move on
    /// after maxWaitSeconds even if the animation misbehaves, so a bad animation state
    /// can no longer soft-lock the whole game.
    /// </summary>
    private IEnumerator WaitForAnimationFinished(ImageAnimation anim, float maxWaitSeconds = 6f)
    {
        float elapsed = 0f;
        while (anim.currentAnimationState != ImageAnimation.ImageState.FINISHED && elapsed < maxWaitSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (anim.currentAnimationState != ImageAnimation.ImageState.FINISHED)
        {
            Debug.LogWarning($"[UIManager] {anim.name} never reached FINISHED within {maxWaitSeconds}s — continuing anyway to avoid a stuck UI.");
        }
    }

    /// <summary>
    /// Plays the big/colossal win popup visuals (title sprite, statue drop, coin/diamond
    /// animations, counting text) for winAmount, animating the counter from startVal.
    /// Does not touch control-enable state or isSpecialWinActive — callers own that.
    /// </summary>
    private IEnumerator PlayBigWinVisuals(double winAmount, double startVal, bool isWheelspin = false)
    {

        double totalBetAmount = gameManager.TotalBetAmount;
        double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;

        double endVal = winAmount;
        double popupWinAmount = winAmount;

        audioController.PlayCollosalWin();

        FirstFireWorkImageAnimation.gameObject.SetActive(true);
        FirstFireWorkImageAnimation.StartAnimation();

        yield return new WaitForSeconds(0.2f);
        if (winPopupPanel) winPopupPanel.SetActive(true);
        BGAnimation.gameObject.SetActive(true);
        BGAnimation.Play(false);

        yield return StartCoroutine(WaitForAnimationFinished(FirstFireWorkImageAnimation));
        FirstFireWorkImageAnimation.gameObject.SetActive(false);

        // Colossal Win >= 50x, Big Win <= 50x
        //Sprite selectedSprite = multiplier >= 50 ? colossalWinSprite : bigWinSprite;
        Sprite selectedSprite;
        if (multiplier <= 50 && !gameManager.isInFreeSpins && !isWheelspin)
        {
            selectedSprite = bigWinSprite;
        }
        else
        {
            selectedSprite = colossalWinSprite;
        }
        if (winPopupTitleImage) winPopupTitleImage.sprite = selectedSprite;
        if (winTitleBG) winTitleBG.SetActive(true);
        if (winPopupTitleImage) winPopupTitleImage.gameObject.SetActive(true);

        if (winPopupText)
        {
            winPopupText.text = "0";
            double currentAnimVal = startVal;
            DOTween.To(() => currentAnimVal, x =>
            {
                currentAnimVal = x;

                double range = endVal - startVal;
                double progress = range > 0 ? Math.Max(0, Math.Min(1, (currentAnimVal - startVal) / range)) : 1.0;

                double currentPopupHit = Math.Round(progress * popupWinAmount, 2);
                winPopupText.text = currentPopupHit.ToString();

                double displayVal = Math.Round(currentAnimVal, 2);
                if (winAmountText) winAmountText.text = displayVal.ToString("F3");

                currentWinDisplayValue = displayVal;
            }, endVal, 2f).SetEase(Ease.OutQuad);
        }

        float popupTime = 8f;
        float animDuration = popupTime - 1f;

        if (winStatueImageRect)
        {
            winStatueImageRect.localPosition = new Vector3(0f, -3000f);
            winStatueImageRect.DOLocalMoveY(3000, 10f).SetEase(Ease.Linear);
            yield return new WaitForSeconds(5f);

            CoinDiamondAnimation.gameObject.SetActive(true);
            CoinDiamondAnimation.Play(false);

            yield return new WaitForSeconds(4f);
            SecondFireWorkImageAnimation.gameObject.SetActive(true);
            SecondFireWorkImageAnimation.StartAnimation();
        }


        yield return new WaitForSeconds(0.7f);

        if (winPopupPanel) winPopupPanel.SetActive(false);
        if (CoinDiamondAnimation) CoinDiamondAnimation.gameObject.SetActive(false);
        if (CoinDiamondAnimation) CoinDiamondAnimation.Stop();
        if (winTitleBG) winTitleBG.SetActive(false);
        if (winPopupTitleImage) winPopupTitleImage.gameObject.SetActive(false);
        if (BGAnimation) BGAnimation.gameObject.SetActive(false);
        if (BGAnimation) BGAnimation.Stop();
        yield return StartCoroutine(WaitForAnimationFinished(SecondFireWorkImageAnimation));
        SecondFireWorkImageAnimation.gameObject.SetActive(false);
    }

    /// <summary>
    /// Post-round win sequence used in two places:
    ///  1. After the last free spin (Beat It or Smooth Criminal) finishes, before returning to normal spins.
    ///  2. After the wheel bonus resolves to a credits/multiplier win (not free games).
    /// Shows the simple win panel first; the moment its background finishes fading to fully
    /// opaque, onBackgroundOpaque fires — the right moment for the caller to swap out
    /// whatever's behind it (masked by the popup instead of flashing through). If winAmount
    /// is >= 50x (or >= 100x) the current bet, the big/colossal win popup plays right after.
    /// The game is fully paused (no spin, no bet change, no autoplay) for the entire sequence.
    /// </summary>
    internal void ShowPostRoundWinSequence(double winAmount, System.Action onBackgroundOpaque, System.Action onComplete)
    {
        StartCoroutine(PostRoundWinRoutine(winAmount, onBackgroundOpaque, onComplete));
    }

    private IEnumerator PostRoundWinRoutine(double winAmount, System.Action onBackgroundOpaque, System.Action onComplete)
    {
        isSpecialWinActive = true;
        DisableControlsDuringWinAnimation();

        if (winAmount > 0)
        {
            yield return StartCoroutine(WinPanelRoutine(winAmount, onBackgroundOpaque));

            double totalBetAmount = gameManager.TotalBetAmount;
            double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;

            // Only Big Win (>= 25x) or Colossal Win (>= 50x) get the extra popup
            //if (multiplier >= 25)
            {
                yield return StartCoroutine(PlayBigWinVisuals(winAmount, 0, true));
            }
        }
        else
        {
            // Nothing to mask the switch with — do it right away.
            onBackgroundOpaque?.Invoke();
        }

        isSpecialWinActive = false;
        EnableControlsAfterWinAnimation();
        onComplete?.Invoke();
    }

    #endregion

    #region Spin Button

    private void OnSpinButtonPressed()
    {
        if (gameManager.isAutoPlaying) return;
        spinButton.interactable = false;
        audioController.PlaySpinButton();
        gameManager.RequestSpin();
    }

    private void OnStopButtonPressed()
    {
        audioController.PlaySpinButton();
        stopButton.interactable = false;
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

        if (autoPlayRotateObject != null)
        {
            if (autoPlayRotationTween != null) autoPlayRotationTween.Kill();
            autoPlayRotationTween = autoPlayRotateObject.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }
    }

    internal void OnAutoPlayStopped()
    {
        if (autoPlayRotationTween != null)
        {
            autoPlayRotationTween.Kill();
            autoPlayRotationTween = null;
        }
        if (autoPlayRotateObject != null)
        {
            autoPlayRotateObject.localRotation = Quaternion.identity;
        }

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
            if (autoPlayButton) autoPlayButton.interactable = false;
        }
    }

    #endregion

    #region Settings Toggles

    private void OnMusicToggleChanged(bool isOn)
    {
        RefreshToggleBgAlpha(musicToggle);
    }

    private void OnSfxToggleChanged(bool isOn)
    {
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
    #region Quit Panel

    private void ShowQuitPanel()
    {
        if (Quit_Panel == null) return;

        Quit_Panel.SetActive(true);
        RectTransform temprect = quitRect;

        if (temprect)
        {
            temprect.localScale = Vector3.zero;
            temprect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void CloseQuitPanel()
    {
        if (Quit_Panel == null || !Quit_Panel.activeSelf) return;

        RectTransform temprect = quitRect;

        if (temprect)
        {
            temprect.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    Quit_Panel.SetActive(false);
                    temprect.localScale = Vector3.one;
                });
        }
        else
        {
            Quit_Panel.SetActive(false);
        }
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

    internal void FreeSpinStartButtonPressed()
    {
        gameManager.FreeGamesIntroPanel.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).OnComplete(() =>
        {
            gameManager.FreeGamesIntroPanel.SetActive(false);
            gameManager.freeSpinTrigger = true;
        });
    }

    internal void OnFreeSpinsStarted(int spinsAwarded, string freeSpinType)
    {
        initialFreeSpins = spinsAwarded;
        totalFreeSpinsAwarded = spinsAwarded;

        // Kill any running win tween (e.g. from wheel bonus win amount) before resetting
        if (winTween != null) { winTween.Kill(); winTween = null; }
        currentWinDisplayValue = 0;
        UpdateWinDisplay(0);

        if (freeSpinType == "beatIt")
        {
            audioController.PlayBeatItStart();
            freeSpinBackground.GetComponent<Image>().sprite = beatItBG;
            //BlurrBg.sprite = beatItBG;
            TitleImage.sprite = beatItTitle;
            IntroTitleImage.sprite = beatItIntroTitle;
        }
        else if (freeSpinType == "smoothCriminal")
        {
            audioController.PlayBeatItStart();
            freeSpinBackground.GetComponent<Image>().sprite = smoothCriminalBG;
            //BlurrBg.sprite = smoothCriminalBG;
            TitleImage.sprite = smoothCriminalTitle;
            IntroTitleImage.sprite = smoothCriminalIntroTitle;
        }

        if (normalSpinBackground) normalSpinBackground.SetActive(false);
        if (freeSpinBackground) freeSpinBackground.SetActive(true);
        if (SlotBg) SlotBg.sprite = FreeSpinSlotBG;

        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(true);
        UpdateFreeSpinCount(spinsAwarded);

        if (spinButton) spinButton.gameObject.SetActive(false);
        if (stopButton) stopButton.gameObject.SetActive(false);
        SetBetControlsEnabled(false);
    }

    internal void OnFreeSpinsEnded(double serverTotalRoundWin, int serverTotalSpinsUsed)
    {
        audioController.PlayBackground();
        gameManager.freeSpinTrigger = false;
        if (normalSpinBackground)
        {
            normalSpinBackground.SetActive(true);
            normalSpinBackground.GetComponentInChildren<ImageAnimation>().StartAnimation();
        }
        //BlurrBg.sprite = NormalBG;
        if (freeSpinBackground) freeSpinBackground.SetActive(false);
        if (SlotBg) SlotBg.sprite = NormalSpinSlotBG;

        TitleImage.sprite = defaultTitleImage;

        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);

        if (spinButton) spinButton.gameObject.SetActive(true);
        if (stopButton) stopButton.gameObject.SetActive(false);
        // Note: bet controls are re-enabled once, at the true end of the win-popup
        // sequence, by EnableControlsAfterWinAnimation — not here, since this now runs
        // mid-sequence (masked behind the opaque win panel) rather than after it.
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
        if (autoPlayRotationTween != null) autoPlayRotationTween.Kill();
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

        StartCoroutine(BonusAnimation());

        wheelBonusPanel.Setup(config, result, (finalResult) =>
        {
            if (finalResult.result.type == "freeGames")
            {
                // Free games: hand off immediately so GameManager can start the free-spin intro.
                wheelBonusPanel.gameObject.SetActive(false);
                onComplete?.Invoke(finalResult);
            }
            else
            {
                // Credits / multiplier win: keep the wheel panel up and run the win panel
                // (plus big/colossal win, if it qualifies) over it. The wheel panel only
                // gets hidden once the win panel is fully opaque, so returning to the
                // normal spin screen is masked instead of flashing through underneath.
                StartCoroutine(WheelBonusWinPanelSequence(finalResult, onComplete));
            }
        });
    }

    private IEnumerator WheelBonusWinPanelSequence(ServerWheelBonusResult result, Action<ServerWheelBonusResult> onComplete)
    {
        isSpecialWinActive = true;
        DisableControlsDuringWinAnimation();

        double winAmount = result.creditAward;

        if (winAmount > 0)
        {
            yield return StartCoroutine(WinPanelRoutine(winAmount, onBackgroundOpaque: () =>
            {
                // The win panel is now fully opaque — safe to hide the wheel bonus panel
                // behind it without the player ever seeing the normal spin screen underneath.
                if (wheelBonusPanel != null) wheelBonusPanel.gameObject.SetActive(false);
                audioController.PlayBackground();
            }));

            double totalBetAmount = gameManager.TotalBetAmount;
            double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;

            //  25x - big win , 50x - collosal win
            //if (multiplier >= 25)
            {
                yield return StartCoroutine(PlayBigWinVisuals(winAmount, 0, true));
            }
        }
        else
        {
            // Nothing to show — just hide the wheel panel and move on.
            if (wheelBonusPanel != null) wheelBonusPanel.gameObject.SetActive(false);
        }

        isSpecialWinActive = false;
        EnableControlsAfterWinAnimation();
        onComplete?.Invoke(result);
    }

    private IEnumerator BonusAnimation()
    {
        WheelStartAnimation.gameObject.SetActive(true);
        audioController.PlayBonusWheelStart();
        ImageAnimation animObj = WheelStartAnimation.GetComponent<ImageAnimation>();
        animObj.StartAnimation();

        yield return new WaitForSeconds(1.5f);
        wheelBonusPanel.gameObject.SetActive(true);
        wheelBonusRect.gameObject.SetActive(true);
        wheelBonusRect.localScale = Vector3.one;

        yield return StartCoroutine(WaitForAnimationFinished(animObj));
        WheelStartAnimation.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        wheelBonusPanel.StartBonus();
    }

    #endregion

    private void UpdateWinDisplay(double amount)
    {
        currentWinDisplayValue = amount;
        if (winAmountText)
            winAmountText.text = amount.ToString("F3");
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
        if (autoPlayButton) autoPlayButton.interactable = (gameManager != null && gameManager.isAutoPlaying) ? true : enabled;
    }

    /// <summary>
    /// Formats a balance value: shows up to 3 decimal places,
    /// but drops trailing zeros and the decimal point for whole numbers.
    /// Examples: 63706.116 → "63706.116", 1000.0 → "1000", 0.025 → "0.025"
    /// </summary>
    private static string FormatBalance(double value)
    {
        return System.Math.Round(value, 3).ToString("F3");
    }
}