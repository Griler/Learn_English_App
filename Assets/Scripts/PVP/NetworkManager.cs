using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MyNetworkManager : MonoBehaviourPunCallbacks
{
    public static MyNetworkManager Instance;
    
    // Biến tạm lưu mã phòng khi đang chờ kết nối
    private string pendingRoomCode = ""; 

    void Awake() 
    { 
        Instance = this; 
        DontDestroyOnLoad(gameObject);
    }

    // Hàm này được gọi từ nút "Đồng Ý"
    public void AttemptToJoinFriendRoom(string roomCode)
    {
        // TRƯỜNG HỢP 1: Đã kết nối sẵn rồi (đang ở sảnh PvP)
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
        {
            Debug.Log("Đã có mạng, vào phòng luôn: " + roomCode);
            PhotonNetwork.JoinRoom(roomCode);
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

    public override void OnJoinedLobby()
    {
        Debug.Log("Đã vào Lobby.");
        
        // Kiểm tra xem có phòng nào đang chờ vào không?
        if (!string.IsNullOrEmpty(pendingRoomCode))
        {
            Debug.Log("Giờ mới bắt đầu vào phòng chờ lúc nãy: " + pendingRoomCode);
            PhotonNetwork.JoinRoom(pendingRoomCode);
            pendingRoomCode = ""; // Reset biến tạm
        }
    }
    
    // Xử lý lỗi nếu phòng đã đầy hoặc không tồn tại
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Vào phòng thất bại: " + message);
        // Ở đây bạn nên hiện thông báo UI: "Phòng không tồn tại hoặc đã đầy"
        pendingRoomCode = "";
    }
}