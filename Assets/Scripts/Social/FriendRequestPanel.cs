using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class FriendRequestPanel : MonoBehaviour
{
    public Transform contentContainer;
    public GameObject requestItemPrefab; // Prefab chứa nút Accept/Decline

    private DatabaseReference _dbRef;

    public void OnShow()
    {
        _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        LoadRequests();
    }

    void LoadRequests()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        string myId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        // Giả sử node chứa request là: users/{myID}/addFriendRequests
        _dbRef.Child($"users/{myId}/addFriendRequests").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                StartCoroutine(SpawnRequestItems(task.Result));
            }
            else
            {
                Debug.Log("Không có lời mời kết bạn nào.");
            }
        });
    }

    IEnumerator SpawnRequestItems(DataSnapshot snapshot)
    {
        foreach (DataSnapshot child in snapshot.Children)
        {
            string senderId = child.Key; // ID người gửi
            
            // Gọi Service lấy thông tin người gửi để hiển thị
            // (Dùng callback để tránh lag như đã tối ưu ở câu trước)
            FriendActionService.Instance.FetchOtherUserInfo(senderId, (info) =>
            {
                GameObject newItem = Instantiate(requestItemPrefab, contentContainer);
                FriendRequestItemUI itemScript = newItem.GetComponent<FriendRequestItemUI>();
                
                // Truyền hàm callback xử lý Accept/Decline vào item
                itemScript.Setup(senderId, info, OnAccept, OnDecline);
            }, 
            (error) => Debug.LogError("Lỗi lấy info request"));

            yield return null; 
        }
    }

    // --- Logic Xử lý khi bấm nút ---

    void OnAccept(string senderId)
    {
        string myId = FirebaseDatabaseManager.Instance.currentUser.UserId;

        Dictionary<string, object> updates = new Dictionary<string, object>();
        
        // Thêm vào danh sách bạn bè (Giả sử cấu trúc friend/userId/list)
        // Lưu ý: Cấu trúc key bên dưới phải khớp với DB của bạn
        updates[$"users/{myId}/friend/userId/{senderId}"] = senderId; 
        updates[$"users/{senderId}/friend/userId/{myId}"] = myId;
        
        // Xóa request
        updates[$"users/{myId}/addFriendRequests/{senderId}"] = null;

        _dbRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Đã chấp nhận kết bạn!");
                // Reload lại danh sách request (hoặc xóa item UI tương ứng)
                OnShow(); 
            }
        });
    }

    void OnDecline(string senderId)
    {
        string myId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        // Chỉ cần xóa request
        _dbRef.Child($"users/{myId}/addFriendRequests/{senderId}").RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
             if (task.IsCompleted) OnShow(); // Reload lại
        });
    }
}