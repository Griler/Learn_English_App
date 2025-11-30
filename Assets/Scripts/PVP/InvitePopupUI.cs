using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using TMPro;

public class InvitePopupUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelObj;
    public TextMeshProUGUI txtMessage;
    public Button btnAccept;
    public Button btnDecline;

    // Lưu thông tin tạm của lời mời hiện tại
    private string currentRoomCode;
    private string currentInviteKey;
    private string currentSenderId; // ID người gửi (để xóa trong db của mình)

    // Hàm hiển thị Popup (Gọi từ FriendSystem)
    public void ShowPopup(string senderName, string roomCode, string inviteKey)
    {
        currentRoomCode = roomCode;
        currentInviteKey = inviteKey;

        txtMessage.text = $"{senderName} mời bạn solo!";
        panelObj.SetActive(true);
    }

    // --- CODE CHO NÚT ĐỒNG Ý ---
    public void OnAcceptClicked()
    {
        Debug.Log("Bạn đã ĐỒNG Ý!");

        // 1. Xóa lời mời trên Firebase ngay lập tức (để không bấm được lần 2)
        RemoveInviteFromFirebase();

        // 2. Ẩn Popup
        panelObj.SetActive(false);

        // 3. Gọi NetworkManager để xử lý việc vào phòng (Connect -> Join)
        MyNetworkManager.Instance.AttemptToJoinFriendRoom(currentRoomCode);
    }

    // --- CODE CHO NÚT TỪ CHỐI ---
    public void OnDeclineClicked()
    {
        Debug.Log("Bạn đã TỪ CHỐI.");

        // 1. Xóa lời mời trên Firebase
        RemoveInviteFromFirebase();

        // 2. Ẩn Popup
        panelObj.SetActive(false);
    }

    // Hàm phụ trợ để xóa data trên Firebase
    private void RemoveInviteFromFirebase()
    {
        if (string.IsNullOrEmpty(currentInviteKey)) return;

        string myUserId = FriendSystem.Instance.myUserId; // Lấy ID của mình
        
        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{myUserId}/invitations/{currentInviteKey}")
            .RemoveValueAsync();
    }
}