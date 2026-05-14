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

    [Header("Spin Settings")]
    [SerializeField] private float spinDuration = 5f;
    [SerializeField] private int extraSpins = 4;
    [SerializeField] private Ease spinEase = Ease.OutCubic;
    [Tooltip("Fine-tune the landing position. Positive shifts clockwise.")]
    [SerializeField] private float alignmentOffset = 0f;

    private float segmentAngle;
    private bool isSpinning;
    private int currentTargetIndex = -1;

    public bool IsSpinning => isSpinning;
    public WheelType WheelType => wheelType;
    public List<WheelSegmentData> SegmentDataList => segments;

    private void Awake()
    {
        Initialize(segments.Count);
    }

    public void Initialize(int segmentCount)
    {
        if (segmentCount <= 0) return;
        segmentAngle = 360f / segmentCount;
    }

    /// <summary>
    /// Spins the wheel to a specific segment index.
    /// </summary>
    public void SpinToIndex(int targetIndex, Action onComplete = null)
    {
        if (isSpinning) return;
        StartCoroutine(SpinRoutine(targetIndex, onComplete));
    }

    private IEnumerator SpinRoutine(int targetIndex, Action onComplete)
    {
        isSpinning = true;
        currentTargetIndex = targetIndex;

        // 1. Calculate the angle for the target segment relative to the wheel's local zero
        float targetSegmentAngle = (targetIndex * segmentAngle) + alignmentOffset;
        float wheelLocalAngle = startOffsetAngle - targetSegmentAngle;
        
        // 2. Get the direction from the wheel center to the arrow
        // (arrow - wheel) gives the vector pointing toward the winning spot on the wheel's edge
        Transform parent = wheelRect.parent;
        Vector3 worldWinningDir = (arrowRect.position - wheelRect.position).normalized;
        Vector3 localWinningDir = parent != null ? parent.InverseTransformDirection(worldWinningDir) : worldWinningDir;
        
        if (localWinningDir.sqrMagnitude < 0.1f) localWinningDir = Vector3.right;
        
        float arrowAngle = Mathf.Atan2(localWinningDir.y, localWinningDir.x) * Mathf.Rad2Deg;
        
        // 3. The target local rotation for the wheel
        float finalTargetLocalRotation = arrowAngle - wheelLocalAngle;

        // 5. Calculate total rotation for Tweening
        float currentLocalRotation = wheelRect.localEulerAngles.z;
        
        // Find the absolute target rotation that is clockwise from current
        float targetAbs = finalTargetLocalRotation;
        while (targetAbs > currentLocalRotation) targetAbs -= 360f;
        
        // Add extra spins
        float totalRotation = targetAbs - (extraSpins * 360f);

        Debug.Log($"[WheelSpin] Target Index: {targetIndex}, Arrow Angle: {arrowAngle:F1}, Target Local Z: {finalTargetLocalRotation:F1}, Total Rotation: {totalRotation:F1}");

        wheelRect.DORotate(
            new Vector3(0, 0, totalRotation),
            spinDuration,
            RotateMode.FastBeyond360
        ).SetEase(spinEase);

        yield return new WaitForSeconds(spinDuration);

        // Snap to perfect alignment
        wheelRect.localRotation = Quaternion.Euler(0, 0, finalTargetLocalRotation);

        isSpinning = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Convenience method to find a segment by a predicate.
    /// </summary>
    public int FindSegmentIndex<T>(List<T> segments, Predicate<T> match)
    {
        return segments.FindIndex(match);
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
