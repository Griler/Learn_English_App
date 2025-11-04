using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class PaginationScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Scroll Settings")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public GameObject viewPoint;
    public List<GameObject> listPage;

    [Header("Pagination Settings")]
    [Range(0, 1)]
    public float snapSpeed = 10f;
    public float swipeThreshold = 50f;
    
    [Header("Page Indicators")]
    public GameObject pageIndicatorPrefab;
    public Transform indicatorParent;
    
    private int currentPage = 0;
    private int totalPage = 0;
    private float[] pagePositions;
    private float widthViewPoint = 0;
    
    private bool isDragging = false;
    private Vector2 dragStartPos;
    
    void Start()
    {
        widthViewPoint = viewPoint.GetComponent<RectTransform>().rect.width;
        
    }
    
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartPos = eventData.position;
        Debug.LogError(isDragging);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // Tính toán hướng swipe
        float dragDistance = eventData.position.x - dragStartPos.x;
        
        if (Mathf.Abs(dragDistance) > swipeThreshold)
        {
            if (dragDistance > 0 && currentPage > 0)
            {
                currentPage--;
            }
            else if (dragDistance < 0 && currentPage < listPage.Count - 1)
            {
                // Swipe left - trang sau
                currentPage++;
            }
        }
        else
        {
            // Snap đến trang gần nhất
            currentPage = GetNearestPage();
        }
        Vector3 targetPosition = new Vector3(-(widthViewPoint * currentPage),17f, 0f);
        // contentPanel.DOMove(targetPosition, 0.1f)
        //     .SetEase(Ease.OutQuad) // Tùy chọn: Đặt kiểu chuyển động (Easing)
        //     .OnComplete(() => {
        //         Debug.Log("Di chuyển World Space hoàn tất!");
        //     });
        
        contentPanel = GetComponent<RectTransform>();
        
        // Di chuyển đến vị trí Neo (Anchored Position) (200, 100)
        contentPanel.DOAnchorPos(new Vector2(200f, 100f), 0.1f)
            .SetEase(Ease.OutBack);
    }
    
    int GetNearestPage()
    {
        float currentPos = scrollRect.horizontalNormalizedPosition;
        float minDistance = float.MaxValue;
        int nearestPage = 0;
        
        for (int i = 0; i < listPage.Count; i++)
        {
            float distance = Mathf.Abs(currentPos - pagePositions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPage = i;
            }
        }
        
        return nearestPage;
    }
    
    
}