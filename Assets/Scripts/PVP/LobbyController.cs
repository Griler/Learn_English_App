using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // Dùng TextMeshPro
using ExitGames.Client.Photon;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable; // Cần để dùng Coroutine

public class LobbyController : MonoBehaviourPunCallbacks
{
    [Header("UI - Buttons Mode")]
    public Button btnMode1; // Nút Mode Trả Lời Câu Hỏi
    public Button btnMode2; // Nút Mode Lật Thẻ
    public Button btnMode3; // Nút Mode Nghe Chọn Ảnh

    [Header("UI - Searching Panel")]
    public GameObject searchingPanel;    // Panel bao gồm Text và nút Hủy
    public TextMeshProUGUI statusText;   // Hiển thị: "Đang tìm...", "Đang tạo..."
    public Button btnCancel;             // Nút Hủy
    public GameObject hoverBlocker;      // Một tấm nền trong suốt để chặn bấm linh tinh

    [Header("Settings")]
    public string waitingRoomScene = "WaitingRoomScene"; // Tên Scene Waiting Room

    // --- BIẾN STATIC ĐỂ XỬ LÝ 'CHƠI TIẾP' ---
    // Biến này được gọi từ GameOverController để báo hiệu cần tìm trận ngay

    // Biến nội bộ
    private int selectedMode = 0;   
    private bool isCanceling = false;
    private const string MODE_KEY = "gm"; // Key định danh chế độ chơi

    // Biến cho cơ chế Retry (Chống tạo 2 phòng cùng lúc)
    private int joinAttempt = 0; 
    private const int MAX_ATTEMPTS = 2; // Thử tìm 2 lần rồi mới tạo

    public override void OnEnable()
    {
        base.OnEnable();
        // 1. Setup UI ban đầu
        searchingPanel.SetActive(false);
        if(hoverBlocker) hoverBlocker.SetActive(false);

        // Khóa nút nếu chưa kết nối
        SetModeButtonsInteractable(PhotonNetwork.IsConnected);

        // 2. Gán sự kiện click cho các nút
        btnMode1.onClick.AddListener(() => OnStartSearch(1, "Đang tìm trận: Trả Lời Câu Hỏi..."));
        btnMode2.onClick.AddListener(() => OnStartSearch(2, "Đang tìm trận: Lật Thẻ Bài..."));
        btnMode3.onClick.AddListener(() => OnStartSearch(3, "Đang tìm trận: Nghe Chọn Ảnh..."));

        btnCancel.onClick.AddListener(OnCancelSearch);
        
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        // Trường hợp 3: Đang lửng lơ (ConnectingToMasterServer) -> CHÍNH LÀ LỖI CỦA BẠN
        else
        {
            // KHÔNG GỌI JOINLOBBY Ở ĐÂY.
            // Hãy im lặng chờ đợi. Khi Photon kết nối xong, nó sẽ tự gọi hàm OnConnectedToMaster bên dưới.
            Debug.Log("Client đang bận kết nối... vui lòng chờ Callback.");
        }
    }

    public override void OnConnected()
    {
        Debug.Log("vao connect");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Lobby: Đã kết nối Master. Đang vào Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Lobby: Đã vào Lobby.");
        SetModeButtonsInteractable(true);
        // --- KIỂM TRA LOGIC AUTO JOIN (CHƠI TIẾP) ---
        if (GlobalData.AutoJoinMode > 0)
        {
            if(NetworkGameState.CurrentJoinType != NetworkGameState.JoinType.RandomMatchmaking) return;
            int mode = GlobalData.AutoJoinMode;
            GlobalData.AutoJoinMode = 0; // Reset ngay để tránh lặp vô hạn

            Debug.Log($"Phát hiện yêu cầu Chơi Tiếp -> Auto tìm Mode {mode}");
            
            string msg = "Đang tìm lại trận...";
            switch (mode)
            {
                case 1: msg = "Đang tìm lại: Trả Lời Câu Hỏi..."; break;
                case 2: msg = "Đang tìm lại: Lật Thẻ Bài..."; break;
                case 3: msg = "Đang tìm lại: Nghe Chọn Ảnh..."; break;
            }
            
            // Tự động kích hoạt tìm trận
            OnStartSearch(mode, msg);
        }
    }

    // --- LOGIC TÌM TRẬN ---

    void OnStartSearch(int mode, string msg)
    {
        NetworkGameState.CurrentJoinType = NetworkGameState.JoinType.RandomMatchmaking;
        selectedMode = mode;
        isCanceling = false;
        joinAttempt = 0; // Reset số lần thử

        // Hiển thị UI
        searchingPanel.SetActive(true);
        statusText.text = msg;
        SetModeButtonsInteractable(false);
        if(hoverBlocker) hoverBlocker.SetActive(true);

        // Bắt đầu tìm kiếm
        JoinRoomByMode();
    }

    // Hàm tìm phòng theo Mode (được tách ra để tái sử dụng khi Retry)
    void JoinRoomByMode()
    {
        Hashtable expectedProps = new Hashtable { { MODE_KEY, selectedMode } };
        // Tham số 0 nghĩa là không lọc theo MaxPlayers (để server tự lo)
        MyNetworkManager.Instance.SetMyUserData();
        PhotonNetwork.JoinRandomRoom(expectedProps, 0);
    }

    // --- LOGIC RETRY & TẠO PHÒNG (QUAN TRỌNG) ---

    // Khi KHÔNG tìm thấy phòng nào phù hợp
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (isCanceling) 
        {
            StopSearchAndClosePanel(); 
            return;
        }

        joinAttempt++; 

        if (joinAttempt < MAX_ATTEMPTS)
        {
            // Chưa thử đủ số lần -> Đợi random rồi thử lại (Để tránh 2 người cùng tạo phòng)
            float randomDelay = Random.Range(0.5f, 1.5f); 
            Debug.Log($"Không thấy phòng. Thử lại lần {joinAttempt} sau {randomDelay}s...");
            statusText.text = $"Đang tìm phòng ({joinAttempt})...";
            
            StartCoroutine(WaitAndRetry(randomDelay));
        }
        else
        {
            // Đã thử hết cách -> Tạo phòng mới
            Debug.Log("Không thấy phòng nào -> Tạo mới.");
            statusText.text = "Đang tạo phòng mới...";
            CreateRoomWithMode();
        }
    }

    IEnumerator WaitAndRetry(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Kiểm tra lại xem người dùng có bấm hủy trong lúc chờ không
        if (!isCanceling)
        {
            JoinRoomByMode(); // Thử tìm lại
        }
    }

    void CreateRoomWithMode()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        options.IsOpen = true;
        options.IsVisible = true;
        
        // Setup Property để người khác tìm được
        options.CustomRoomProperties = new Hashtable() { { MODE_KEY, selectedMode } };
        options.CustomRoomPropertiesForLobby = new string[] { MODE_KEY };

        string roomName = "Room_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Tạo phòng lỗi: " + message);
        // Nếu lỗi tạo phòng, thử retry lại từ đầu
        if (!isCanceling) StartCoroutine(WaitAndRetry(1f));
    }

    // --- KHI VÀO PHÒNG THÀNH CÔNG ---

    public override void OnJoinedRoom()
    {
        // Trường hợp hiếm: Vừa tìm thấy thì người dùng bấm Hủy
        if (isCanceling)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }
        if (NetworkGameState.CurrentJoinType != NetworkGameState.JoinType.RandomMatchmaking)
        {
            return;
        }

        Debug.Log("Đã vào phòng thành công -> Chuyển sang WaitingRoom");
        PhotonNetwork.LoadLevel(waitingRoomScene);
    }

    // --- XỬ LÝ HỦY (CANCEL) ---

    void OnCancelSearch()
    {
        isCanceling = true;
        statusText.text = "Đang hủy...";
        StopAllCoroutines(); // Dừng việc retry
        
        // Đảm bảo xóa cờ AutoJoin để không bị lặp lại nếu quay lại Lobby
        GlobalData.AutoJoinMode = 0; 

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // Nếu chưa vào phòng (đang kết nối hoặc đang retry) thì tắt UI luôn
            StopSearchAndClosePanel();
        }
    }

    public override void OnLeftRoom()
    {
        // Callback khi thoát phòng thành công (do bấm hủy)
        StopSearchAndClosePanel();
    }

    void StopSearchAndClosePanel()
    {
        isCanceling = false;
        searchingPanel.SetActive(false);
        SetModeButtonsInteractable(true);
        if(hoverBlocker) hoverBlocker.SetActive(false);
    }

    void SetModeButtonsInteractable(bool state)
    {
        btnMode1.interactable = state;
        btnMode2.interactable = state;
        btnMode3.interactable = state;
    }
}