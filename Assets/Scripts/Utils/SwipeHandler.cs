using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// FIX: Now orientation-aware. SwipeHandler detects downward swipes for the bonus wheel.
// In landscape mode, that's a downward Y-axis swipe.
// In portrait mode (UIWrapper rotated -90 degrees), a visual downward swipe becomes 
// a leftward swipe in screen coordinates, so we check the X-axis instead.
public class SwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public UnityEvent OnSwipeDown;
    public float minSwipeDistance = 50f;

    private Vector2 startPosition;
    private bool swipeDetected = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[SwipeHandler] BeginDrag fired, pointerId={eventData.pointerId}, isLandscape={OrientationChange.IsLandscapeOrientation}");
        startPosition = eventData.position;
        swipeDetected = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (swipeDetected) return;

        Vector2 currentPosition = eventData.position;
        bool swipeTriggered = false;

        if (OrientationChange.IsLandscapeOrientation)
        {
            // Landscape: check for downward swipe (top to bottom = positive distance down Y-axis)
            float distance = startPosition.y - currentPosition.y;
            swipeTriggered = distance > minSwipeDistance;
        }
        else
        {
            // Portrait: UIWrapper is rotated -90 degrees. A visual downward swipe becomes 
            // a leftward swipe in screen coordinates. Check X-axis (left = negative).
            float distance = startPosition.x - currentPosition.x;
            swipeTriggered = distance > minSwipeDistance;
        }

        if (swipeTriggered)
        {
            swipeDetected = true;
            Debug.Log($"[SwipeHandler] Swipe detected! isLandscape={OrientationChange.IsLandscapeOrientation}");
            OnSwipeDown?.Invoke();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Reset on drag end for next gesture
        swipeDetected = false;
    }
}