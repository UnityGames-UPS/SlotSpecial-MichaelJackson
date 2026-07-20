using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class OrientationChange : MonoBehaviour
{
  [SerializeField] private RectTransform UIWrapper;
  [SerializeField] private CanvasScaler CanvasScaler;

  [SerializeField] private float transitionDuration = 0.2f;
  [SerializeField] private float waitForRotation = 0.2f;

  private Vector2 ReferenceAspect;
  private Tween matchTween;
  private Tween rotationTween;
  private Coroutine rotationRoutine;
  private bool isLandscape;
  private bool hasAppliedInitialMatch = false; // FIX: tracks whether the very first ApplyMatch has run

  // FIX: expose current orientation state so SwipeHandler can adapt its swipe detection axis
  public static bool IsLandscapeOrientation { get; private set; } = true;

  // FIX: exposes whether UIWrapper's rotation/CanvasScaler match is mid-tween, so systems that
  // read world-space transform positions (e.g. WheelSpinController) can wait for it to settle
  // instead of capturing a mid-transition value.
  public static bool IsTransitioning { get; private set; } = false;

  private void Awake()
  {
    ReferenceAspect = CanvasScaler.referenceResolution;
  }

  private void Start()
  {
    ApplyMatch(Screen.width, Screen.height);
  }

  private void SwitchDisplay(string dimensions)
  {
    if (rotationRoutine != null) StopCoroutine(rotationRoutine);
    rotationRoutine = StartCoroutine(RotationCoroutine(dimensions));
  }

  private IEnumerator RotationCoroutine(string dimensions)
  {
    yield return new WaitForSecondsRealtime(waitForRotation);
    string[] parts = dimensions.Split(',');
    if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height) && width > 0 && height > 0)
    {
      ApplyMatch(width, height);
    }
    else
    {
      Debug.LogWarning("Unity: Invalid format received in SwitchDisplay");
    }
  }

  private void ApplyMatch(int width, int height)
  {
    isLandscape = width > height;
    IsLandscapeOrientation = isLandscape; // FIX: update static state for swipe handlers

    Quaternion targetRotation = isLandscape ? Quaternion.identity : Quaternion.Euler(0, 0, -90);
    if (rotationTween != null && rotationTween.IsActive()) rotationTween.Kill();

    float refW = ReferenceAspect.x;
    float refH = ReferenceAspect.y;

    float widthScale = (float)width / refW;
    float heightScale = (float)height / refH;

    float targetScale;
    if (isLandscape)
    {
      targetScale = Mathf.Min(widthScale, heightScale);
    }
    else
    {
      
      
      float portraitWidthScale = (float)height / refW;
      float portraitHeightScale = (float)width / refH;
      targetScale = Mathf.Min(portraitWidthScale, portraitHeightScale);
    }

    float targetMatch;
    if (Mathf.Abs(heightScale - widthScale) < 0.0001f)
    {
      targetMatch = 0.5f;
    }
    else
    {
      float logRatio = Mathf.Log(heightScale / widthScale);
      targetMatch = Mathf.Log(targetScale / widthScale) / logRatio;
      targetMatch = Mathf.Clamp01(targetMatch);
    }

    if (matchTween != null && matchTween.IsActive()) matchTween.Kill();

    // FIX: the very first orientation application (game launch) has no visible "before" state
    // for the player to see transition from, so apply it instantly. This avoids a window where
    // world-space positions under UIWrapper are mid-rotation/mid-scale when something else
    // (like WheelSpinController) reads them right after the game becomes interactive.
    if (!hasAppliedInitialMatch)
    {
      hasAppliedInitialMatch = true;
      UIWrapper.localRotation = targetRotation;
      CanvasScaler.matchWidthOrHeight = targetMatch;
      IsTransitioning = false;
    }
    else
    {
      IsTransitioning = true;
      rotationTween = UIWrapper.DOLocalRotateQuaternion(targetRotation, transitionDuration).SetEase(Ease.OutCubic);
      matchTween = DOTween.To(() => CanvasScaler.matchWidthOrHeight, x => CanvasScaler.matchWidthOrHeight = x, targetMatch, transitionDuration)
        .SetEase(Ease.InOutQuad)
        .OnComplete(() => IsTransitioning = false); // FIX: clear flag once settled
    }

    Debug.LogWarning($"Unity: Dimensions {width}x{height}, isLandscape: {isLandscape}, targetMatch calculated: {targetMatch}");
  }

#if UNITY_EDITOR
  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Space))
    {
      SwitchDisplay(Screen.width + "," + Screen.height);
    }
  }
#endif
}