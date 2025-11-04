using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Nested Scroll Rect")]
public class NestedScrollRect : ScrollRect
{
    [Header("Nested Scroll Settings")]
    public ScrollRect parentScrollRect;

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        base.OnInitializePotentialDrag(eventData);
        parentScrollRect?.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect == null)
        {
            base.OnBeginDrag(eventData);
            return;
        }

        if (vertical && Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x))
            base.OnBeginDrag(eventData);
        else if (horizontal && Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
            parentScrollRect.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (parentScrollRect == null)
        {
            base.OnDrag(eventData);
            return;
        }

        if (vertical && Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x))
            base.OnDrag(eventData);
        else if (horizontal && Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
            parentScrollRect.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        parentScrollRect?.OnEndDrag(eventData);
    }
}