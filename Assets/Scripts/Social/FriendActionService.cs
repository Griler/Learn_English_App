using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

// Service này chuyên xử lý các hành động liên quan đến bạn bè
public class FriendActionService : MonoBehaviour
{
    public static FriendActionService Instance { get; private set; }

    private DatabaseReference dbRef;
    // ID của chính mình (cần set cái này khi login thành công)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // --- HÀNH ĐỘNG ---

    // 1. Thêm bạn bè (Ghi vào node friend của mình)
    public void AddFriend(string friendIdToAdd, Action<bool, string> callback)
    {
        string MyCurrentUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        if (string.IsNullOrEmpty(friendIdToAdd) || friendIdToAdd == MyCurrentUserId)
        {
            callback?.Invoke(false, "ID không hợp lệ");
            return;
        }

        // Cấu trúc lưu: users/myId/friend/friendId/userId = friendId
        // Dùng friendId làm Key để dễ xóa sau này
        dbRef.Child("users").Child(MyCurrentUserId).Child("friend").Child(friendIdToAdd).Child("userId").SetValueAsync(friendIdToAdd)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) callback?.Invoke(false, task.Exception.Message);
                else callback?.Invoke(true, "Thêm thành công!");
            });
    }

    // 2. Xóa bạn bè
    public void RemoveFriend(string friendIdToRemove, Action action)
    {
        if (FirebaseDatabaseManager.Instance == null || FirebaseDatabaseManager.Instance.currentUser == null) return;
        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        string myId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        Dictionary<string, object> updates = new Dictionary<string, object>();
        
        string pathRemoveFromMe = $"users/{myId}/friend/userId/{friendIdToRemove}";
        string pathRemoveFromFriend = $"users/{friendIdToRemove}/friend/userId/{myId}";
        updates[pathRemoveFromMe] = null;
        updates[pathRemoveFromFriend] = null;
        // 3. Gửi lệnh cập nhật 1 lần duy nhất (Atomic Update)
        dbRef.UpdateChildrenAsync(updates).ContinueWith(task => 
        {
            if (task.IsCompleted)
            {
                Debug.Log($"✅ Đã xóa kết bạn 2 chiều thành công với: {friendIdToRemove}");
                action?.Invoke();
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("❌ Lỗi khi xóa bạn: " + task.Exception);
            }
        });
    }

    // 3. Mời PvP (Giả lập)
    public void InvitePvP(string friendId)
    {
        Debug.Log($"[PVP] Đang gửi lời mời đến: {friendId}...");
    }

    // --- QUAN TRỌNG: LẤY INFO NGƯỜI KHÁC ---

    // Hàm này lấy thông tin user info của một ID bất kỳ (dùng callback để trả kết quả)
    public void FetchOtherUserInfo(string userId, Action<UserInfoData> onSuccess, Action<string> onError)
    {
        dbRef.Child("users").Child(userId).Child("userInfo").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                onError?.Invoke("Không tìm thấy user hoặc lỗi mạng.");
                return;
            }

            string json = task.Result.GetRawJsonValue();
            UserInfoData data = JsonUtility.FromJson<UserInfoData>(json);
            onSuccess?.Invoke(data);
        });
    }
}