using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public UnityEvent OnSwipeDown;
    public float minSwipeDistance = 50f;

    private Vector2 startPosition;
    private bool swipeDetected = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = eventData.position;
        swipeDetected = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (swipeDetected) return;

        Vector2 currentPosition = eventData.position;
        float distance = startPosition.y - currentPosition.y; // Top to bottom is positive distance

        if (distance > minSwipeDistance)
        {
            swipeDetected = true;
            OnSwipeDown?.Invoke();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Not strictly needed if we detect during drag, but good for completeness
    }
}
