using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class FriendAddPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField searchInputField;
    public Button searchButton;
    public Button sendRequestsButton;
    public TextMeshProUGUI statusText; // Text thông báo lỗi/thành công

    [Header("Result List")]
    public Transform resultContainer; // Content của ScrollView chứa user tìm thấy
    public GameObject friendSearchItemPrefab; // Prefab gán script FriendSearchItemUI

    // Danh sách tạm chứa các User ID đã tìm thấy và chuẩn bị kết bạn
    private List<string> _usersToSendRequest = new List<string>();
    private DatabaseReference _dbRef;

    void Start()
    {
        _dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        searchButton.onClick.AddListener(OnSearchClicked);
        sendRequestsButton.onClick.AddListener(OnSendRequestsClicked);

        // Reset UI khi bắt đầu
    }

    public void OnShow()
    {
        statusText.text = "";
        sendRequestsButton.interactable = false;
    }

    private void OnEnable()
    {
        // Xóa trắng dữ liệu mỗi khi mở panel lên
        ClearPanel();
    }

    // --- 1. LOGIC TÌM KIẾM ---
    void OnSearchClicked()
    {
        string searchId = searchInputField.text.Trim();
        string currentUserId = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.username;
        
        // Validate cơ bản
        if (string.IsNullOrEmpty(searchId))
        {
            statusText.text = "Vui lòng nhập ID.";
            return;
        }
        if (searchId == currentUserId)
        {
            statusText.text = "Không thể kết bạn với chính mình.";
            return;
        }
        if (_usersToSendRequest.Contains(searchId))
        {
            statusText.text = "Người này đã có trong danh sách gửi.";
            return;
        }

        // Check Firebase xem User có tồn tại không
        // Giả sử searchId là cái người dùng nhập vào (VD: "User_Dragon")
        _dbRef.Child("users")
            .OrderByChild("userInfo/username") // HOẶC "userInfo/searchID" tùy field bạn muốn tìm
            .EqualTo(searchId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Lỗi tìm kiếm: " + task.Exception);
                    statusText.text = "Lỗi kết nối.";
                    return;
                }

                DataSnapshot snapshot = task.Result;

                // LƯU Ý: Với Query, snapshot trả về là một DANH SÁCH (kể cả chỉ tìm thấy 1 người)
                if (snapshot.Exists && snapshot.ChildrenCount > 0)
                {
                    // Phải dùng vòng lặp để lấy user ra
                    foreach (DataSnapshot userSnapshot in snapshot.Children)
                    {
                        // userSnapshot chính là cái node: users/UID_CUA_USER
                        string foundName = "Unknown";
            
                        if (userSnapshot.Child("userInfo").Child("username").Exists)
                        {
                            foundName = userSnapshot.Child("userInfo").Child("username").Value.ToString();
                        }
                        else if (userSnapshot.Child("username").Exists)
                        {
                            foundName = userSnapshot.Child("username").Value.ToString();
                        }
                        Debug.Log("Tìm thấy: " + foundName);
                        AddUserToTempList(userSnapshot.Key); 
                    }

                    statusText.text = ""; 
                    searchInputField.text = ""; 
                }
                else
                {
                    statusText.text = $"Không tìm thấy người chơi: {searchId}";
                }
            });
    }

    // Thêm vào list tạm và hiển thị lên UI
    void AddUserToTempList(string id)
    {
        // BƯỚC 1: Gọi Service lấy data trước (chưa tạo UI vội)
        FriendActionService.Instance.FetchOtherUserInfo(id, 
            (info) => {
                // --- THÀNH CÔNG ---
            
                // 1. Giờ mới add vào list logic quản lý
                _usersToSendRequest.Add(id);
                sendRequestsButton.interactable = true;
            
                // 2. Instantiate và gán thẳng vào resultContainer (Parent)
                // Vì data đã load xong nên nó sẽ hiện đầy đủ ngay lập tức
                GameObject newItem = Instantiate(friendSearchItemPrefab, resultContainer);
            
                FriendSearchItemUI itemScript = newItem.GetComponent<FriendSearchItemUI>();
                if (itemScript != null)
                {
                    // Truyền cục data (info) đã lấy được vào Setup
                    itemScript.Setup(id, info, this); 
                }
            },
            (error) => {
                // --- THẤT BẠI ---
                Debug.LogError("Lỗi lấy thông tin user: " + id);
                // Không tạo prefab -> User không thấy item lỗi hiện lên
            }
        );
    }

    // Hàm public để script Item gọi khi bấm nút Xóa
    public void RemoveUserFromList(string id)
    {
        if (_usersToSendRequest.Contains(id))
        {
            _usersToSendRequest.Remove(id);
        }

        if (_usersToSendRequest.Count == 0)
        {
            sendRequestsButton.interactable = false;
        }
    }

    // --- 2. LOGIC GỬI REQUEST (BULK) ---
    void OnSendRequestsClicked()
    {
        if (_usersToSendRequest.Count == 0) return;

        string currentUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        Dictionary<string, object> updates = new Dictionary<string, object>();

        // Tạo update cho tất cả user trong list
        // Cấu trúc: users -> [FriendID] -> addFriendRequests -> [MyID] = "pending" (hoặc true/timestamp)
        foreach (string targetId in _usersToSendRequest)
        {
            string path = $"users/{targetId}/addFriendRequests/{currentUserId}";
            updates[path] = "pending"; // Hoặc DateTime.Now.ToString()
        }

        // Gửi 1 lần (Atomic update) hoặc gửi từng cái. Ở đây dùng UpdateChildrenAsync cho tối ưu
        _dbRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                statusText.text = "Đã gửi lời mời kết bạn thành công!";
                ClearPanel();
            }
            else
            {
                statusText.text = "Gửi thất bại: " + task.Exception;
            }
        });
    }

    void ClearPanel()
    {
        _usersToSendRequest.Clear();
        foreach (Transform child in resultContainer)
        {
            Destroy(child.gameObject);
        }
        searchInputField.text = "";
        sendRequestsButton.interactable = false;
    }
}