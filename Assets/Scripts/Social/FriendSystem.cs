using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;

public class FriendSystem : MonoBehaviour
{
    public static FriendSystem Instance;

    [Header("User Info (Giả lập)")]
    public string myUserId = ""; // ID của chính mình (thực tế lấy từ Auth)
    public string myUserName = "";

    [Header("References")]
    public InvitePopupUI invitePopup; // Kéo cái UI Popup vào đây

    private DatabaseReference myInvitesRef;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ListenForInvites();
    }

    // =========================================================
    // PHẦN 1: GỬI LỜI MỜI (Sender Logic)
    // =========================================================
    
    public void SendInvite(string friendId, string friendName)
    {
        myUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        // 1. Tạo một mã phòng ngẫu nhiên (Ví dụ: Room_4821)
        string roomCode = "Room_" + Random.Range(1000, 9999);
        Debug.Log($"Đang gửi lời mời tới {friendId} vào phòng {roomCode}");

        // 2. Gửi dữ liệu lên Firebase của BẠN BÈ
        DatabaseReference friendRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{friendId}/invitations");

        Dictionary<string, object> inviteData = new Dictionary<string, object>();
        inviteData["senderName"] = friendName;
        inviteData["roomCode"] = roomCode;

        friendRef.Push().SetValueAsync(inviteData);

        // 3. QUAN TRỌNG: Chính mình (người mời) cũng phải vào phòng đó!
        // Gọi hàm xử lý kết nối thông minh bên NetworkManager
        MyNetworkManager.Instance.AttemptToJoinFriendRoom(roomCode);
    }

    // =========================================================
    // PHẦN 2: NHẬN LỜI MỜI (Receiver Logic)
    // =========================================================

    void ListenForInvites()
    {
        myUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        myInvitesRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{myUserId}/invitations");

        // Lắng nghe sự kiện: Có tin nhắn mới là báo ngay
        myInvitesRef.ChildAdded += OnNewInviteReceived;
    }

    void OnNewInviteReceived(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Value == null) return;

        var data = args.Snapshot.Value as Dictionary<string, object>;
        
        // Lấy thông tin an toàn (tránh lỗi null)
        string senderName = data.ContainsKey("senderName") ? data["senderName"].ToString() : "Unknown";
        string roomCode = data.ContainsKey("roomCode") ? data["roomCode"].ToString() : "";
        string inviteKey = args.Snapshot.Key;

        Debug.Log($"Có thư mời từ {senderName}!");

        // Hiện Popup lên màn hình để người chơi quyết định
        invitePopup.ShowPopup(senderName, roomCode, inviteKey);
    }

    // Gọi hàm này khi muốn hủy lắng nghe (VD: lúc thoát app)
    void OnDestroy()
    {
        if (myInvitesRef != null)
        {
            myInvitesRef.ChildAdded -= OnNewInviteReceived;
        }
    }
}