using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class WheelBonusPanel : MonoBehaviour
{
    [Header("Audio Controller")]
    [SerializeField] private AudioController audioController;

    [Header("Wheels")]
    [SerializeField] private WheelSpinController mainWheel;
    [SerializeField] private WheelSpinController miniWheel;

    [Header("UI Elements")]
    [SerializeField] private GameObject mainWheelArrow;
    [SerializeField] private GameObject miniWheelArrow;
    [SerializeField] private TextMeshProUGUI winAmountText;
    [SerializeField] private SwipeHandler swipeHandler;
    [SerializeField] private GameObject startInstructionArea;
    [SerializeField] private GameObject SparkleAnimObj;
    [SerializeField] private ImageAnimation MiniWheelStartAnim;
    [SerializeField] private TMP_Text FreeSpinCountText;

    [Header("Settings")]
    [SerializeField] private float delayBetweenWheels = 1f;

    private ServerWheelBonusResult resultData;
    private WheelBonusConfig config;
    private Action<ServerWheelBonusResult> onComplete;

    private void Awake()
    {


        if (swipeHandler != null)
        {
            swipeHandler.OnSwipeDown.AddListener(OnSwipeDetected);
        }
    }

    private void OnSwipeDetected()
    {
        if (spinTriggered) return;
        OnSpinClicked();
    }

    private bool spinTriggered = false;

    private void OnSpinClicked()
    {
        spinTriggered = true;
        if (swipeHandler != null) swipeHandler.gameObject.SetActive(false);
        //if (startInstructionArea != null) startInstructionArea.SetActive(false);
    }

    internal void Setup(WheelBonusConfig config, ServerWheelBonusResult result, Action<ServerWheelBonusResult> onComplete)
    {
        this.config = config;
        this.resultData = result;
        this.onComplete = onComplete;

        // 1. Setup Main Wheel Segments from Server Config
        if (mainWheel.WheelType == WheelType.MainWheel && config != null && config.wheelSegments != null)
        {
            // Group server segments by type
            var serverCredits = config.wheelSegments.FindAll(s => s.type == "credits");
            var serverMultipliers = config.wheelSegments.FindAll(s => s.type == "multiplierWheel");
            var serverFreeGames = config.wheelSegments.FindAll(s => s.type == "freeGames");

            int creditIdx = 0, multiIdx = 0, freeIdx = 0;

            foreach (var seg in mainWheel.SegmentDataList)
            {
                if (seg.type == WheelSegmentType.Credits)
                {
                    // If we run out of server segments, cycle back to the beginning
                    if (creditIdx >= serverCredits.Count) creditIdx = 0;

                    if (serverCredits.Count > 0)
                    {
                        var data = serverCredits[creditIdx++];
                        seg.assignedValue = data.value;
                        if (seg.valueText != null) seg.valueText.text = data.value.ToString();
                    }
                }
                else if (seg.type == WheelSegmentType.MultiplierWheel)
                {
                    if (multiIdx >= serverMultipliers.Count) multiIdx = 0;

                    if (serverMultipliers.Count > 0)
                    {
                        var data = serverMultipliers[multiIdx++];
                        seg.assignedValue = data.value;
                        if (seg.valueText != null) seg.valueText.text = data.value.ToString(); //+ " Extra";
                    }
                }
                else if (seg.type == WheelSegmentType.FreeGames)
                {
                    // Match based on featureName (e.g. "beatIt")
                    var data = config.wheelSegments.Find(s => s.type == "freeGames" && s.feature == seg.featureName);
                    if (data != null)
                    {
                        seg.assignedValue = data.count ?? 0;
                        //if (seg.valueText != null) seg.valueText.text = seg.featureName;
                    }
                }
            }
        }

        // 2. Setup Mini Wheel Segments (Multipliers)
        if (miniWheel.WheelType == WheelType.MiniWheel && config != null && config.multiplierWheelSegments != null)
        {
            var segments = miniWheel.SegmentDataList;
            foreach (var seg in segments)
            {
                // Use the serverIndex assigned in the inspector
                if (seg.serverIndex >= 0 && seg.serverIndex < config.multiplierWheelSegments.Count)
                {
                    seg.assignedValue = config.multiplierWheelSegments[seg.serverIndex];
                    // Text is baked into UI, so no text update needed
                }
            }
        }

        winAmountText.text = "";
        miniWheel.gameObject.SetActive(false);
        miniWheelArrow.SetActive(false);

        spinTriggered = false;
        if (swipeHandler != null) swipeHandler.gameObject.SetActive(true);
        swipeHandler.GetComponent<ImageAnimation>().StartAnimation();
        if (startInstructionArea != null) startInstructionArea.SetActive(true);
    }

    internal void StartBonus()
    {
        StartCoroutine(BonusSequence());
    }

    private IEnumerator BonusSequence()
    {
        audioController.PlayBonusWheelBackground();
        // 0. Wait for user to tap/slide to spin
        yield return new WaitUntil(() => spinTriggered);

        // 1. Find target index on main wheel
        int mainTargetIndex = FindMainWheelTargetIndex();

        if (mainTargetIndex == -1)
        {
            Debug.LogError("Could not find matching segment on main wheel!");
            onComplete?.Invoke(resultData);
            yield break;
        }

        // 2. Spin Main Wheel
        bool mainSpinDone = false;
        mainWheel.SpinToIndex(mainTargetIndex, () => mainSpinDone = true);

        yield return new WaitUntil(() => mainSpinDone);
        yield return new WaitForSeconds(0.5f);
        if (resultData.result.type != "multiplierWheel")
        {
            audioController.PlayBonusWheelSparkle();
            SparkleAnimObj.SetActive(true);
            SparkleAnimObj.GetComponent<ImageAnimation>().StartAnimation();
            yield return new WaitUntil(() => SparkleAnimObj.GetComponent<ImageAnimation>().currentAnimationState == ImageAnimation.ImageState.FINISHED);
        }

        // 3. Check if we need to spin the mini wheel (Multiplier scenario)
        if (resultData.result.type == "multiplierWheel")
        {
            audioController.PlayBonusTrackMultiplier();
            MiniWheelStartAnim.gameObject.SetActive(true);
            MiniWheelStartAnim.StartAnimation();
            yield return new WaitUntil(() => MiniWheelStartAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);
            MiniWheelStartAnim.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            CanvasGroup cg = miniWheel.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            miniWheel.gameObject.SetActive(true);
            cg.DOFade(1f, 1f).SetEase(Ease.InOutSine);
            yield return new WaitForSeconds(1f);
            miniWheelArrow.SetActive(true);
            yield return new WaitForSeconds(delayBetweenWheels);

            int miniTargetIndex = FindMiniWheelTargetIndex();

            audioController.PlayInnerWheel();
            bool miniSpinDone = false;
            miniWheel.SpinToIndex(miniTargetIndex, () => miniSpinDone = true);

            yield return new WaitUntil(() => miniSpinDone);
            audioController.PlayBonusWheelSparkle();
            yield return new WaitForSeconds(0.5f);
        }

        // 4. Show result and auto-close
        ShowWinAmount();
        yield return new WaitForSeconds(1.5f); // Let the player see the result
        OnCollectClicked();
    }

    private int FindMainWheelTargetIndex()
    {
        var res = resultData.result;
        List<int> matches = new List<int>();
        var segments = mainWheel.SegmentDataList;

        WheelSegmentType targetType = WheelSegmentType.Credits;
        if (res.type == "freeGames") targetType = WheelSegmentType.FreeGames;
        else if (res.type == "multiplierWheel") targetType = WheelSegmentType.MultiplierWheel;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.type == targetType)
            {
                if (seg.type == WheelSegmentType.Credits && Math.Abs(seg.assignedValue - res.value) < 0.01f)
                    matches.Add(i);
                else if (seg.type == WheelSegmentType.FreeGames && seg.featureName == res.feature)
                    matches.Add(i);
                else if (seg.type == WheelSegmentType.MultiplierWheel && Math.Abs(seg.assignedValue - res.value) < 0.01f)
                    matches.Add(i);
            }
        }

        if (matches.Count > 0)
            return matches[UnityEngine.Random.Range(0, matches.Count)];

        Debug.LogError($"[WheelBonusPanel] No segment found matching type {res.type} value {res.value}");
        return -1;
    }

    private int FindMiniWheelTargetIndex()
    {
        if (resultData.multiplierResult == null) return 0;

        int targetVal = resultData.multiplierResult.Value;
        List<int> matches = new List<int>();
        var segments = miniWheel.SegmentDataList;

        for (int i = 0; i < segments.Count; i++)
        {
            if (Math.Abs(segments[i].assignedValue - targetVal) < 0.01f)
            {
                matches.Add(i);
            }
        }

        if (matches.Count > 0)
            return matches[UnityEngine.Random.Range(0, matches.Count)];

        return 0;
    }

    private void ShowWinAmount()
    {
        if (resultData.result.type == "credits" || resultData.result.type == "multiplierWheel")
        {
            winAmountText.text = $"{resultData.creditAward}";
        }
        else if (resultData.result.type == "freeGames")
        {
            //FreeGamesIntroPanel.SetActive(true);
            FreeSpinCountText.text = $"{resultData.result.count} "; // FREE GAMES!";
        }
    }

    private void OnCollectClicked()
    {
        onComplete?.Invoke(resultData);
    }
}
