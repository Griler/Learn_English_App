using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MyNetworkManager : MonoBehaviourPunCallbacks
{
    public static MyNetworkManager Instance;
    public UserProfileSO userProfileSo;
    // Biến tạm lưu mã phòng khi đang chờ kết nối
    private string pendingRoomCode = ""; 
    private void Awake()
    {
        // 1. Kiểm tra Singleton: Nếu đã có Instance rồi mà không phải là "tôi" -> Tự hủy
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; // Dừng code tại đây
        }

        // 2. Gán Instance
        Instance = this;
    
        // 3. Giữ Object này tồn tại khi chuyển Scene (Quan trọng cho Network Manager)
        DontDestroyOnLoad(this.gameObject);

        // 4. Thiết lập Photon (Yêu cầu của bạn)
        // Giúp tất cả client tự động load scene theo Master Client
        PhotonNetwork.AutomaticallySyncScene = true; 
    }
    
    public void SetMyUserData()
    {
        PhotonNetwork.NickName = userProfileSo.userInfo.name; // Set tên hiển thị của Photon

        // Tạo bảng chứa thông tin mở rộng
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["AvatarID"] = userProfileSo.userInfo.avatar;
        props["BorderID"] = userProfileSo.userInfo.border;
        props["Rank"] = userProfileSo.userInfo.rankPoint.ToString();
        props["IsReady"] = false; // Mặc định vào phòng là chưa Ready
    
        // Đẩy lên mạng
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private Action onJoinRoomCallBack; 
    // Hàm này được gọi từ nút "Đồng Ý"
    public void AttemptToJoinFriendRoom(string roomCode, Action onJoinRoomCB)
{
    // --- 1. SET UP DỮ LIỆU ---
    if (NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.RandomMatchmaking)
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
    }

    NetworkGameState.CurrentJoinType = NetworkGameState.JoinType.FriendInvite;
    onJoinRoomCallBack = onJoinRoomCB;
    SetMyUserData();
    
    // Lưu mã phòng ngay lập tức để dù đi đường nào cũng có dữ liệu
    pendingRoomCode = roomCode; 

    // --- 2. XỬ LÝ LOGIC MẠNG (Sửa lại đoạn này) ---

    // TRƯỜNG HỢP A: Đã có kết nối mạng (Bất kể đang ở trạng thái nào)
    if (PhotonNetwork.IsConnected)
    {
        Debug.Log("Đã có kết nối mạng. Trạng thái hiện tại: " + PhotonNetwork.NetworkClientState);

        // Nếu đang kẹt trong phòng nào đó -> Rời ngay
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Đang kẹt trong phòng, tiến hành rời phòng...");
            PhotonNetwork.LeaveRoom();
            return; // Đợi callback OnConnectedToMaster xử lý tiếp
        }

        // Nếu đã ở trong Lobby -> Vào phòng luôn
        if (PhotonNetwork.InLobby)
        {
            Debug.Log("Đang ở Lobby, vào phòng: " + roomCode);
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 2;
            roomOptions.IsVisible = false;
            PhotonNetwork.JoinOrCreateRoom(roomCode, roomOptions, TypedLobby.Default);
        }
        // Nếu đã kết nối Master nhưng chưa vào Lobby -> Vào Lobby
        else if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
        {
            Debug.Log("Đang ở Master, vào Lobby...");
            PhotonNetwork.JoinLobby();
        }
        // Các trạng thái lấp lửng khác (Disconnecting, Authenticating...) -> Đợi nó tự ổn định
        else
        {
             Debug.Log("Mạng đang bận xử lý (State: " + PhotonNetwork.NetworkClientState + "), chờ một chút...");
             // Thường thì nó sẽ tự nhảy về OnConnectedToMaster, lúc đó pendingRoomCode đã lưu rồi nên sẽ tự chạy tiếp.
        }
    }
    // TRƯỜNG HỢP B: Chưa có kết nối mạng (Disconnected)
    else
    {
        Debug.Log("Chưa có mạng (Disconnected), bắt đầu kết nối...");
        PhotonNetwork.ConnectUsingSettings();
    }
}

    // --- CÁC CALLBACK CỦA PHOTON ---

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedRoom()
    {
        if (NetworkGameState.CurrentJoinType != NetworkGameState.JoinType.FriendInvite)
        {
            return;
        }
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("WaitingRoomScene");
        }
        onJoinRoomCallBack?.Invoke();
    }
    public override void OnJoinedLobby()
    {
        Debug.Log("Đã vào Lobby.");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // Giới hạn 2 người
        roomOptions.IsVisible = false;
        // Kiểm tra xem có phòng nào đang chờ vào không?
        if ((!string.IsNullOrEmpty(pendingRoomCode)&& 
            NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.FriendInvite))
        {
            
            Debug.Log("Giờ mới bắt đầu vào phòng chờ lúc nãy: " + pendingRoomCode);
            PhotonNetwork.JoinOrCreateRoom(pendingRoomCode,roomOptions, TypedLobby.Default);
            pendingRoomCode = ""; // Reset biến tạm
        }
    }

    // Xử lý lỗi nếu phòng đã đầy hoặc không tồn tại
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Region hiện tại: " + PhotonNetwork.CloudRegion);

        Debug.LogError("Vào phòng thất bại: " + message);
        switch (returnCode)
        {
            case ErrorCode.GameDoesNotExist:
                Debug.Log("Lỗi: Phòng này không còn tồn tại (Chủ phòng đã out?).");
                ToastSystem.Instance.ShowToast("Phòng này không còn tồn tại");
                break;
            
            case ErrorCode.GameFull: 
                Debug.Log("Lỗi: Phòng đã đủ người.");
                ToastSystem.Instance.ShowToast("Phòng đã đầy");
                break;
            
            case ErrorCode.GameClosed:
                Debug.Log("Lỗi: Phòng đang chơi, không cho vào.");   
                ToastSystem.Instance.ShowToast("Phòng đang chơi, không cho vào");
                break;
            
            default:
                Debug.Log("Lỗi lạ khác");
                break;
        }
        // Ở đây bạn nên hiện thông báo UI: "Phòng không tồn tại hoặc đã đầy"
        pendingRoomCode = "";
        onJoinRoomCallBack?.Invoke();
    }
}