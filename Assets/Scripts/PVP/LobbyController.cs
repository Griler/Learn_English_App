using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // Sử dụng TextMeshPro
using ExitGames.Client.Photon; // Cần thư viện này để dùng Hashtable

public class LobbyController : MonoBehaviourPunCallbacks
{
    [Header("UI - Game Mode Buttons")]
    public Button btnMode1; // Ví dụ: 1vs1 Thường
    public Button btnMode2; // Ví dụ: 1vs1 Xếp hạng
    public Button btnMode3; // Ví dụ: Chế độ đặc biệt

    [Header("UI - Searching Panel")]
    public GameObject searchingPanel;    // Panel chứa thông báo và nút hủy
    public TextMeshProUGUI statusText;   // Text hiển thị "Đang tìm Mode 1..."
    public Button btnCancel;             // Nút Hủy tìm
    public TextMeshProUGUI btnText;             // Nút Hủy tìm
    public GameObject hover;
    [Header("Settings")]
    public string waitingRoomScene = "WaitingRoomScene"; // Tên Scene phòng chờ

    // Biến lưu trạng thái
    private int selectedMode = 0;   // Lưu chế độ người chơi vừa chọn
    private bool isCanceling = false;

    // Key để định danh chế độ chơi trên Server
    private const string MODE_KEY = "gm"; 

    void Start()
    {
        // 1. Setup ban đầu
        searchingPanel.SetActive(false); // Ẩn panel tìm trận đi
        
        // Disable các nút khi chưa kết nối xong
        SetModeButtonsInteractable(false);

        // 2. Gán sự kiện cho các nút
        // Dùng Lambda expression để truyền tham số mode vào hàm
        btnMode1.onClick.AddListener(() => OnStartSearch(1, "Tìm trận Trả Lời Câu Hỏi"));
        btnMode2.onClick.AddListener(() => OnStartSearch(2, "Tìm trận Lật Thẻ Bài"));
        btnMode3.onClick.AddListener(() => OnStartSearch(3, "Tìm trận Nghe Chon Ảnh"));

        btnCancel.onClick.AddListener(OnCancelSearch);

        // 3. Kết nối Photon
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            OnConnectedToMaster(); // Nếu đã kết nối rồi thì mở khóa nút luôn
        }
    }

    // --- LOGIC UI & NÚT BẤM ---

    // Hàm gọi khi bấm 1 trong 3 nút chọn chế độ
    void OnStartSearch(int mode, string statusMsg)
    {
        NetworkGameState.CurrentJoinType = NetworkGameState.JoinType.RandomMatchmaking;
        selectedMode = mode;
        isCanceling = false;

        // Hiện Panel tìm kiếm
        searchingPanel.SetActive(true);
        statusText.text = statusMsg;
        btnText.text = "Huỷ";

        // Khóa các nút chọn mode lại để không bấm lung tung
        SetModeButtonsInteractable(false);

        // --- PHOTON LOGIC: TÌM PHÒNG THEO MODE ---
        // Tạo bộ lọc: Chỉ tìm phòng nào có Property "gm" == mode mình chọn
        Hashtable expectedCustomRoomProperties = new Hashtable { { MODE_KEY, selectedMode } };
        
        // 0 là maxPlayers (0 = không check số lượng ở bước lọc này, server tự check đầy phòng)
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);
        hover.SetActive(true);
    }

    void OnCancelSearch()
    {
        isCanceling = true;
        
        // Thay vì tắt ngay, ta thông báo cho người dùng biết hệ thống đang xử lý
        statusText.text = "Đang hủy...";
        btnCancel.interactable = false; // Khóa nút h
    }
    
    void StopSearchAndClosePanel()
    {
        isCanceling = false;
        searchingPanel.SetActive(false); // Tắt panel
        SetModeButtonsInteractable(true); // Mở lại các nút chọn mode
        btnCancel.interactable = true;
        btnText.text = "Tìm Trận";
        hover.SetActive(true); // Reset nút hủy cho lần sau
        Debug.Log("Đã hủy tìm trận hoàn tất.");
    }

    void SetModeButtonsInteractable(bool state)
    {
        btnMode1.interactable = state;
        btnMode2.interactable = state;
        btnMode3.interactable = state;
    }

    // --- PHOTON CALLBACKS ---

    public override void OnConnectedToMaster()
    {
        Debug.Log("Đã kết nối tới Server!");
        MyNetworkManager.Instance.SetMyUserData();

    }
    // Thêm hàm này (hoặc sửa hàm cũ) để bật nút đúng thời điểm
    public override void OnJoinedLobby()
    {
        Debug.Log("LobbyController: Đã thực sự vào Lobby -> Bật nút tìm trận.");
        SetModeButtonsInteractable(true);
    }

    // 1. Tìm thấy phòng (Khớp Mode) -> Vào luôn
    public override void OnJoinedRoom()
    {
        if (NetworkGameState.CurrentJoinType != NetworkGameState.JoinType.RandomMatchmaking)
        {
            return;
        }
        
        // Kiểm tra xem người dùng có vừa bấm Hủy không
        if (isCanceling)
        {
            statusText.text = "Đang thoát phòng vừa tìm thấy...";
            Debug.Log("Lỡ vào phòng khi đang hủy -> Rời phòng ngay.");
            PhotonNetwork.LeaveRoom(); 
            return;
        }

        PhotonNetwork.LoadLevel("WaitingRoomScene");
    }

    // 2. Không tìm thấy phòng nào (của Mode này) -> Tạo phòng mới
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (isCanceling)
        {
            StopSearchAndClosePanel();
            return;
        }

        // Nếu không hủy -> Logic tạo phòng như cũ
        Debug.Log("Không thấy phòng, tạo mới...");
        statusText.text = "Đang tạo phòng mới...";
        CreateRoomWithMode();
    }
    
    public override void OnLeftRoom()
    {
        // Khi đã thoát phòng thành công -> Coi như quy trình hủy hoàn tất
        if (isCanceling)
        {
            StopSearchAndClosePanel();
        }
    }

    void CreateRoomWithMode()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        options.IsOpen = true;
        options.IsVisible = true;

        // --- CẤU HÌNH QUAN TRỌNG ĐỂ TÁCH CHẾ ĐỘ CHƠI ---
        // 1. Gán nhãn cho phòng này là Mode mấy
        options.CustomRoomProperties = new Hashtable() { { MODE_KEY, selectedMode } };
        
        // 2. Bắt buộc phải khai báo key này cho Lobby biết để còn lọc được
        options.CustomRoomPropertiesForLobby = new string[] { MODE_KEY };
        string roomName = "Match_" + System.Guid.NewGuid().ToString();
        PhotonNetwork.CreateRoom(roomName, options);
    }
    
    // Xử lý khi tạo phòng thất bại (ví dụ trùng tên - hiếm gặp khi để null)
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (isCanceling)
        {
            StopSearchAndClosePanel();
            return;
        }     
        Debug.LogError("Tạo phòng lỗi: " + message);
        // Thử lại hoặc báo lỗi UI
        Invoke("OnCancelSearch", 2f); // Tự hủy sau 2s
    }

  
}