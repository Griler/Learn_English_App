using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MyNetworkManager : MonoBehaviourPunCallbacks
{
    public static MyNetworkManager Instance;
    public UserProfileSO userProfileSo;
    // Biến tạm lưu mã phòng khi đang chờ kết nối
    private string pendingRoomCode = ""; 

    void Awake() 
    { 
        Instance = this; 
        PhotonNetwork.AutomaticallySyncScene = true; // CỰC KỲ QUAN TRỌNG
        DontDestroyOnLoad(gameObject);
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

    // Hàm này được gọi từ nút "Đồng Ý"
    public void AttemptToJoinFriendRoom(string roomCode)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // Giới hạn 2 người
        roomOptions.IsVisible = false;
        SetMyUserData();
        // TRƯỜNG HỢP 1: Đã kết nối sẵn rồi (đang ở sảnh PvP)
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
        {
            Debug.Log("Đã có mạng, vào phòng luôn: " + roomCode);
            pendingRoomCode = roomCode;
            PhotonNetwork.JoinOrCreateRoom(roomCode,roomOptions, TypedLobby.Default);
        }
        // TRƯỜNG HỢP 2: Chưa kết nối (Đang ở menu học bài)
        else
        {
            Debug.Log("Chưa có mạng, đang kết nối để vào phòng: " + roomCode);
            pendingRoomCode = roomCode; // Lưu lại mã phòng để dùng sau
            PhotonNetwork.ConnectUsingSettings(); // Bắt đầu kết nối
        }
    }

    // --- CÁC CALLBACK CỦA PHOTON ---

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedRoom()
    {
        // Nếu là Host (người tạo phòng) thì load vào phòng chờ
        // Client sẽ tự đi theo nhờ PhotonNetwork.AutomaticallySyncScene = true
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("WaitingRoomScene");
        }
    }
    public override void OnJoinedLobby()
    {
        Debug.Log("Đã vào Lobby.");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // Giới hạn 2 người
        roomOptions.IsVisible = false;
        // Kiểm tra xem có phòng nào đang chờ vào không?
        if (!string.IsNullOrEmpty(pendingRoomCode))
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
        // Ở đây bạn nên hiện thông báo UI: "Phòng không tồn tại hoặc đã đầy"
        pendingRoomCode = "";
    }
}