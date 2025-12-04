using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
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
    private Vector3 oldPosition;
    // Hàm hiển thị Popup (Gọi từ FriendSystem)
    private void Start()
    {
        oldPosition = panelObj.transform.position;
        btnAccept.onClick.AddListener(OnAcceptClicked);
        btnDecline.onClick.AddListener(OnDeclineClicked);
        
    }

    private void OnEnable()
    {
        GameEvents.showInvitePopup += ShowPopup;
        FirebaseDatabaseManager.Instance.OnFirebaseInitialized += RemoveInviteFromFirebase;
    }

    public void ShowPopup(string senderName, string roomCode, string inviteKey)
    {
        currentRoomCode = roomCode;
        currentInviteKey = inviteKey;

        txtMessage.text = $"{senderName} mời bạn solo!";
        panelObj.SetActive(true);
        Vector3 targetPoint = Vector3.zero;
        panelObj.GetComponent<RectTransform>().DOAnchorPos(targetPoint, .5f).SetEase(Ease.OutQuad);
    }

    // --- CODE CHO NÚT ĐỒNG Ý ---
    public void OnAcceptClicked()
    {
        Debug.Log("Bạn đã ĐỒNG Ý!");

        // 1. Xóa lời mời trên Firebase ngay lập tức (để không bấm được lần 2)
        //RemoveInviteFromFirebase();
        // 2. Ẩn Popup
        panelObj.SetActive(false);
        string myUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        DatabaseReference inviteRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{myUserId}/invitations/{currentInviteKey}");
        // 3. Gọi NetworkManager để xử lý việc vào phòng (Connect -> Join)
        inviteRef.Child("status").SetValueAsync("accepted").ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted)
            {
                panelObj.SetActive(false);
                MyNetworkManager.Instance.AttemptToJoinFriendRoom(currentRoomCode, RemoveInviteFromFirebase);
            }
        });
    }

    // --- CODE CHO NÚT TỪ CHỐI ---
    public void OnDeclineClicked()
    {
        Debug.Log("Bạn đã TỪ CHỐI.");

        // 1. Xóa lời mời trên Firebase
        RemoveInviteFromFirebase();

        panelObj.GetComponent<RectTransform>().position = oldPosition;
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

    private void OnDisable()
    {
        GameEvents.showInvitePopup -= ShowPopup;
        FirebaseDatabaseManager.Instance.OnFirebaseInitialized -= RemoveInviteFromFirebase;
    }
}