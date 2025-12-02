using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions; // Cần thiết để dùng ContinueWithOnMainThread

public class OtherUserProfilePopup : MonoBehaviour
{
    [Header("UI References")] public Button closeButton;
    public Button inviteButton;
    public GameObject invitePanel; // Panel nhỏ mời bạn

    [Header("List References")] public Transform contentContainer; // Object "Content" trong ScrollView
    public GameObject friendItemPrefab; // Prefab FriendItem đã tạo ở bước 2

    [Header("Firebase Config")] public string firebasePath = "users/friend_list"; // Đường dẫn trên DB

    private DatabaseReference _dbRef;
    
    void Start()
    {
        // 1. Khởi tạo Firebase Reference
        _dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 2. Gán sự kiện cho nút
        closeButton.onClick.AddListener(ClosePopup);
        inviteButton.onClick.AddListener(OpenInvitePanel);

        // Mặc định ẩn invite panel
        invitePanel.SetActive(false);

        // 3. Load danh sách bạn bè
        LoadFriendList();
    }

    void LoadFriendList()
    {
        // Xóa list cũ
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        // BƯỚC 1: Lấy danh sách ID bạn bè
        // Đường dẫn: users -> [CurrentID] -> friend -> userId
        string currentUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        string pathListFriend = $"users/{currentUserId}/friend/userId";

        _dbRef.Child(pathListFriend).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.HasChildren)
                {
                    foreach (DataSnapshot childNode in snapshot.Children)
                    {
                        string friendId = childNode.Value.ToString();
                        SpawnFriendItem(friendId);
                    }
                }
                else
                {
                    Debug.Log("Không có bạn bè nào.");
                }
            }
        });
    }
    
    void SpawnFriendItem(string data)
    {
        GameObject newItem = Instantiate(friendItemPrefab, contentContainer);
        var itemScript = newItem.GetComponent<FriendItemUI>(); // Script cũ ở câu trả lời trước
        if (itemScript != null)
        {
            //itemScript.SetupUI(data);
        }
    }

    void OpenInvitePanel()
    {
        invitePanel.SetActive(true);
        // Có thể thêm logic load dữ liệu cho invite panel ở đây nếu cần
    }

    public void CloseInvitePanel()
    {
        invitePanel.SetActive(false);
    }

    void ClosePopup()
    {
        // Ẩn Popup này đi
        gameObject.SetActive(false);
        // Hoặc Destroy(gameObject) tùy cách bạn quản lý UI
    }
}