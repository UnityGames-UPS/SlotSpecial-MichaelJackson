using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using TMPro;

public enum WheelType
{
    MainWheel,
    MiniWheel
}

public enum WheelSegmentType
{
    Credits,
    FreeGames,
    MultiplierWheel,
    MiniMultiplier
}

[Serializable]
public class WheelSegmentData
{
    public WheelSegmentType type;
    [Tooltip("Only for FreeGames type")] public string featureName;
    public TextMeshProUGUI valueText;

    [HideInInspector] public double assignedValue;
    public int serverIndex = -1;
}

public class WheelSpinController : MonoBehaviour
{
    [Header("Wheel Configuration")]
    [SerializeField] private WheelType wheelType;
    [SerializeField] private RectTransform wheelRect;
    [SerializeField] private RectTransform arrowRect;

    [Header("Segments (CLOCKWISE ORDER)")]
    [SerializeField] private List<WheelSegmentData> segments = new List<WheelSegmentData>();

    [Header("Angle Settings")]
    [Tooltip("Angle where segment[0] center is located. 90 = Top.")]
    [SerializeField] private float startOffsetAngle = 90f;

    // FIX: previously this was computed live every spin from arrowRect/wheelRect world positions.
    // That's fragile the moment the two aren't perfectly rigid siblings under an identically
    // rotated ancestor (e.g. once OrientationChange rotates UIWrapper for portrait) — any small
    // anchor/pivot asymmetry between the wheel and the arrow silently produces a wrong angle.
    // The arrow never actually moves relative to the wheel at runtime, so this is a design
    // constant: measure it once in Edit Mode (with the wheel at its resting/zero rotation) via
    // the "Calibrate Arrow Angle From Scene" context menu below, and it never needs to be
    // recomputed again regardless of any runtime UI rotation.
    [Tooltip("Angle (same convention as startOffsetAngle) where the arrow points, relative to the wheel's own local zero rotation. Use the context menu to calibrate.")]
    [SerializeField] private float arrowAngle = 90f;

    [Header("Spin Settings")]
    [SerializeField] private float spinDuration = 5f;
    [SerializeField] private int extraSpins = 4;
    [SerializeField] private Ease spinEase = Ease.OutCubic;
    [Tooltip("Fine-tune the landing position. Positive shifts clockwise.")]
    [SerializeField] private float alignmentOffset = 0f;

    private float segmentAngle;
    private bool isSpinning;
    private int currentTargetIndex = -1;

    internal bool IsSpinning => isSpinning;
    internal WheelType WheelType => wheelType;
    internal List<WheelSegmentData> SegmentDataList => segments;

    private void Awake()
    {
        Initialize(segments.Count);
    }

    internal void Initialize(int segmentCount)
    {
        if (segmentCount <= 0) return;
        segmentAngle = 360f / segmentCount;
    }

#if UNITY_EDITOR
    // FIX: run this once in Edit Mode (with wheelRect at its resting/zero local rotation) to
    // measure arrowAngle from the actual scene layout, replacing guesswork. Right-click the
    // WheelSpinController component header and choose "Calibrate Arrow Angle From Scene".
    [ContextMenu("Calibrate Arrow Angle From Scene")]
    private void CalibrateArrowAngleFromScene()
    {
        if (wheelRect == null || arrowRect == null)
        {
            Debug.LogWarning("[WheelSpin] Assign wheelRect and arrowRect before calibrating.");
            return;
        }

        if (Mathf.Abs(wheelRect.localEulerAngles.z) > 0.01f)
        {
            Debug.LogWarning($"[WheelSpin] wheelRect.localEulerAngles.z is {wheelRect.localEulerAngles.z:F2}, not 0. " +
                "Calibrate with the wheel at its resting/zero rotation, otherwise the measured value won't be a valid constant.");
        }

        Transform parent = wheelRect.parent;
        Vector3 worldWinningDir = (arrowRect.position - wheelRect.position).normalized;
        Vector3 localWinningDir = parent != null ? parent.InverseTransformDirection(worldWinningDir) : worldWinningDir;
        if (localWinningDir.sqrMagnitude < 0.1f) localWinningDir = Vector3.right;

        float measuredAngle = Mathf.Atan2(localWinningDir.y, localWinningDir.x) * Mathf.Rad2Deg;
        arrowAngle = measuredAngle;

        Debug.Log($"[WheelSpin] Calibrated arrowAngle = {measuredAngle:F2}");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    internal void SpinToIndex(int targetIndex, Action onComplete = null)
    {
        if (isSpinning) return;
        LogArrowAlignmentDiagnostic(); // TEMP DIAGNOSTIC — remove once root cause confirmed
        StartCoroutine(SpinRoutine(targetIndex, onComplete));
    }

    // TEMP DIAGNOSTIC: recomputes the wheel->arrow angle live, using the exact same formula
    // as the Edit-mode "Calibrate Arrow Angle From Scene" context menu, and logs it next to the
    // stored calibrated constant. If both objects are truly rigid siblings under an ancestor
    // that only rotates as a whole (no independent rotation anywhere in between), this live
    // value should match the stored `arrowAngle` in EVERY orientation, landscape or portrait.
    // If it doesn't match — and especially if the delta is ~90 in portrait — something between
    // wheelRect/arrowRect and their common rotated ancestor has its own independent rotation
    // that isn't shared identically by both objects.
    private void LogArrowAlignmentDiagnostic()
    {
        if (wheelRect == null || arrowRect == null) return;

        Transform parent = wheelRect.parent;
        Vector3 worldWinningDir = (arrowRect.position - wheelRect.position).normalized;
        Vector3 localWinningDir = parent != null ? parent.InverseTransformDirection(worldWinningDir) : worldWinningDir;
        if (localWinningDir.sqrMagnitude < 0.1f) localWinningDir = Vector3.right;

        float liveAngle = Mathf.Atan2(localWinningDir.y, localWinningDir.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(arrowAngle, liveAngle);

        Debug.LogWarning(
            "========== [WheelSpin][DIAG] ==========\n" +
            $"isLandscape          = {OrientationChange.IsLandscapeOrientation}\n" +
            $"StoredArrowAngle     = {arrowAngle:F2}\n" +
            $"LiveArrowAngle       = {liveAngle:F2}\n" +
            $"Delta (live-stored)  = {delta:F2}   <-- if this is ~90, that's our bug\n" +
            $"wheelRect.parent     = {(parent != null ? parent.name : "null")}\n" +
            $"parent.lossyScale    = {parent?.lossyScale}\n" +
            $"parent.eulerAngles(world) = {parent?.eulerAngles}\n" +
            $"wheelRect.localEulerAngles.z = {wheelRect.localEulerAngles.z:F2}\n" +
            "========================================");
    }

    private IEnumerator SpinRoutine(int targetIndex, Action onComplete)
    {
        isSpinning = true;
        currentTargetIndex = targetIndex;

        // 1. Calculate the angle for the target segment relative to the wheel's local zero
        float targetSegmentAngle = (targetIndex * segmentAngle) + alignmentOffset;
        float wheelLocalAngle = startOffsetAngle - targetSegmentAngle;

        // 2. The target local rotation for the wheel
        // FIX: arrowAngle is now a fixed, calibrated design constant (see field tooltip) instead
        // of a live world-position read — removes any dependency on UIWrapper's runtime rotation.
        float finalTargetLocalRotation = arrowAngle - wheelLocalAngle;

        // 5. Calculate total rotation for Tweening
        float currentLocalRotation = wheelRect.localEulerAngles.z;

        // Find the absolute target rotation that is clockwise from current
        float targetAbs = finalTargetLocalRotation;
        while (targetAbs > currentLocalRotation) targetAbs -= 360f;

        // Add extra spins
        float totalRotation = targetAbs - (extraSpins * 360f);

        Debug.Log($"[WheelSpin] Target Index: {targetIndex}, SegmentAngle: {segmentAngle:F2}, ArrowAngle: {arrowAngle:F1}, WheelLocalAngle: {wheelLocalAngle:F1}, TargetLocalZ: {finalTargetLocalRotation:F1}, CurrentZ(start): {currentLocalRotation:F1}, TotalRotation: {totalRotation:F1}");

        // FIX: previously we raced a WaitForSeconds(spinDuration) coroutine timer against
        // DOTween's own internal clock, then force-completed the tween and manually snapped
        // rotation. Those are two independent timers that aren't guaranteed to agree on which
        // frame "done" happens, and a manually-forced Complete's autokill/cleanup can still run
        // later in DOTween's own update pass, after our manual snap line executed, silently
        // overwriting it back to a stale value. Fix: let the tween's own OnComplete drive the
        // snap, so it's guaranteed to be the last write to wheelRect for this spin.
        bool tweenDone = false;

        if (OrientationChange.IsLandscapeOrientation)
        {
            wheelRect.DORotate(
            new Vector3(0, 0, totalRotation ),
            spinDuration,
            RotateMode.FastBeyond360
        ).SetEase(spinEase)
         .OnComplete(() =>
         {
             Debug.Log($"[WheelSpin] Tween OnComplete, pre-snap wheel Z: {wheelRect.localEulerAngles.z:F1}, Snapping to: {finalTargetLocalRotation:F1} (mod360: {((finalTargetLocalRotation % 360f) + 360f) % 360f:F1})");

             // Snap to perfect alignment
             wheelRect.localRotation = Quaternion.Euler(0, 0, finalTargetLocalRotation);
             tweenDone = true;
         });
        }
        else
        {
            wheelRect.DORotate(
                new Vector3(0, 0, totalRotation - 90f),
                spinDuration,
                RotateMode.FastBeyond360
            ).SetEase(spinEase)
             .OnComplete(() =>
             {
                 Debug.Log($"[WheelSpin] Tween OnComplete, pre-snap wheel Z: {wheelRect.localEulerAngles.z:F1}, Snapping to: {finalTargetLocalRotation:F1} (mod360: {((finalTargetLocalRotation % 360f) + 360f) % 360f:F1})");

                 // Snap to perfect alignment
                 wheelRect.localRotation = Quaternion.Euler(0, 0, finalTargetLocalRotation);
                 tweenDone = true;
             });
        }
        yield return new WaitUntil(() => tweenDone);

        isSpinning = false;
        onComplete?.Invoke();
    }



    private void OnDrawGizmos()
    {
        DrawWheelGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawWheelGizmos(true);
    }

    private void DrawWheelGizmos(bool selected)
    {
        if (wheelRect == null) return;

        int count = (segments != null && segments.Count > 0) ? segments.Count : 18; // Default to 18 if list is empty
        float angleStep = 360f / count;
        Vector3 center = wheelRect.position;

        // Calculate radius based on RectTransform size and scale
        float radius = (wheelRect.rect.width > 0) ? (wheelRect.rect.width * 0.5f * wheelRect.lossyScale.x) : 100f;

        Gizmos.color = selected ? Color.white : new Color(1, 1, 1, 0.2f);
        Gizmos.DrawWireSphere(center, radius * 0.05f);

        for (int i = 0; i < count; i++)
        {
            float angle = startOffsetAngle - (i * angleStep) - alignmentOffset;
            float rad = angle * Mathf.Deg2Rad;
            // Local direction
            Vector3 localDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            // World direction
            Vector3 worldDir = wheelRect.rotation * localDir;

            if (selected)
            {
                if (currentTargetIndex != -1 && i == currentTargetIndex)
                    Gizmos.color = Color.magenta; // Winning segment
                else
                    Gizmos.color = (i == 0) ? Color.green : Color.red;

                Gizmos.DrawLine(center, center + worldDir * radius);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(center + worldDir * radius * 1.05f, i.ToString());
#endif
            }
            else
            {
                Gizmos.color = new Color(1, 1, 1, 0.1f);
                Gizmos.DrawLine(center, center + worldDir * radius * 0.5f);
            }
        }

        if (arrowRect != null)
        {
            Gizmos.color = selected ? Color.yellow : new Color(1, 0.92f, 0.016f, 0.3f);
            Vector3 arrowPos = arrowRect.position;
            // Direction from wheel center to arrow (World Space for Gizmos)
            Vector3 worldWinningDir = (arrowPos - wheelRect.position).normalized;
            Vector3 endPos = wheelRect.position + worldWinningDir * radius;

            // Draw line from center to arrow position on edge
            Gizmos.DrawLine(wheelRect.position, endPos);

            if (selected)
            {
                // Draw arrow head
                float headSize = radius * 0.05f;
                Vector3 right = Vector3.Cross(worldWinningDir, Vector3.forward).normalized;
                Vector3 headLeft = endPos - worldWinningDir * headSize + right * headSize * 0.5f;
                Vector3 headRight = endPos - worldWinningDir * headSize - right * headSize * 0.5f;

                Gizmos.DrawLine(endPos, headLeft);
                Gizmos.DrawLine(endPos, headRight);
                Gizmos.DrawLine(headLeft, headRight);
            }
        }
    }
}