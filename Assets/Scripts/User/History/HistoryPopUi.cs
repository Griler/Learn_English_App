using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro; // Để dùng Math

public class HistoryPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject historyPanel;
    public Transform contentContainer;
    
    [Header("Prefabs")]
    public GameObject historyWinItemPrefab;  
    public GameObject historyLoseItemPrefab; 

    [Header("Pagination UI")]
    public Button nextButton;      // Kéo nút Next vào đây
    public Button prevButton;      // Kéo nút Prev vào đây
    public TextMeshProUGUI pageNumberText;    // Kéo Text hiển thị số trang vào đây
    public TextMeshProUGUI statusText;    // Kéo Text hiển thị số trang vào đây

    [Header("Manager")]
    public HistoryManager historyManager;

    // --- CÁC BIẾN QUẢN LÝ PHÂN TRANG ---
    private List<MatchHistoryData> fullHistoryList = new List<MatchHistoryData>(); // Lưu toàn bộ list tải về
    private int currentPage = 0;        // Trang hiện tại (bắt đầu từ 0)
    private const int ITEMS_PER_PAGE = 3; // Số item mỗi trang

    private void Start()
    {
        // Gán sự kiện cho nút bấm (hoặc gán trong Inspector cũng được)
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
    }

    private void OnEnable()
    {
        OpenHistoryPopup();
    }

    public void OpenHistoryPopup()
    {
        historyPanel.SetActive(true);
        RefreshHistory();
    }

    public void CloseHistoryPopup()
    {
        historyPanel.SetActive(false);
    }

    void RefreshHistory()
    {
        statusText.gameObject.SetActive(false);
        // Tải dữ liệu từ Firebase
        historyManager.LoadHistory((dataList) =>
        {
            if (dataList.Count == 0)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "Chưa có lịch sử thi đấu".ToUpper();
            }
            // 1. Lưu toàn bộ dữ liệu vào biến list
            fullHistoryList = dataList;
            
            // 2. Reset về trang đầu tiên
            currentPage = 0;

            // 3. Hiển thị trang đầu
            RenderCurrentPage();
        },onErrorLoadHistory);
    }

    void onErrorLoadHistory()
    {
        statusText.gameObject.SetActive(true);
        statusText.text = "Tải lịch sử đấu lỗi".ToUpper();
        
    }

    // --- HÀM XỬ LÝ HIỂN THỊ TRANG ---
    void RenderCurrentPage()
    {
        // 1. Xóa các item cũ đang hiển thị
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Tính toán vị trí bắt đầu và kết thúc của trang hiện tại
        // Ví dụ: Trang 0 (0-3), Trang 1 (4-7)...
        int startIndex = currentPage * ITEMS_PER_PAGE;
        
        // Điểm kết thúc không được vượt quá tổng số phần tử
        int endIndex = Math.Min(startIndex + ITEMS_PER_PAGE, fullHistoryList.Count);

        // 3. Vòng lặp chỉ chạy trong khoảng đã cắt (Logic Win/Lose của bạn ở đây)
        for (int i = startIndex; i < endIndex; i++)
        {
            MatchHistoryData match = fullHistoryList[i];
            GameObject newItem;

            if (match.rankChange > 0)
            {
                newItem = Instantiate(historyWinItemPrefab, contentContainer);
            }     
            else if (match.rankChange == 0)
            {
                newItem = Instantiate(historyWinItemPrefab, contentContainer);
            }
            else
            {
                newItem = Instantiate(historyLoseItemPrefab, contentContainer);
            }

            HistoryItemUI itemScript = newItem.GetComponent<HistoryItemUI>();
            if (itemScript != null)
            {
                itemScript.SetData(match);
            }
        }

        // 4. Cập nhật trạng thái nút bấm và Text
        UpdatePaginationUI();
    }

    void UpdatePaginationUI()
    {
        // Tính tổng số trang
        // Công thức: (Tổng item + số item mỗi trang - 1) / số item mỗi trang
        int totalPages = (fullHistoryList.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        if (totalPages == 0) totalPages = 1; // Tránh lỗi chia 0 hoặc hiển thị 0/0

        // Hiển thị text: Trang hiện tại (cộng thêm 1 cho user dễ hiểu) / Tổng trang
        if(pageNumberText != null)
            pageNumberText.text = $"Trang {currentPage + 1}/{totalPages}";

        // Ẩn/Hiện hoặc Disable nút bấm
        // Nếu đang ở trang đầu (0) -> Không bấm được nút Prev
        prevButton.interactable = (currentPage > 0);

        // Nếu đang ở trang cuối -> Không bấm được nút Next
        nextButton.interactable = (currentPage < totalPages - 1);
    }

    // --- HÀM SỰ KIỆN NÚT BẤM ---
    public void NextPage()
    {
        // Kiểm tra xem có phải trang cuối chưa
        int totalPages = (fullHistoryList.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RenderCurrentPage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RenderCurrentPage();
        }
    }
}