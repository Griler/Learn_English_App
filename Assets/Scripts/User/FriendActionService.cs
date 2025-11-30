using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System;

// Service này chuyên xử lý các hành động liên quan đến bạn bè
public class FriendActionService : MonoBehaviour
{
    public static FriendActionService Instance { get; private set; }

    private DatabaseReference dbRef;
    // ID của chính mình (cần set cái này khi login thành công)
    public string MyCurrentUserId = "user_123"; 

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
    public void RemoveFriend(string friendIdToRemove)
    {
        // Xóa node: users/myId/friend/friendIdToRemove
        dbRef.Child("users").Child(MyCurrentUserId).Child("friend").Child(friendIdToRemove).RemoveValueAsync();
        Debug.Log($"Đã gửi lệnh xóa bạn: {friendIdToRemove}");
        // Không cần callback, vì FirebaseFetcher đang lắng nghe node này, 
        // khi xóa xong trên server, nó sẽ tự động báo về và UI tự cập nhật.
    }

    // 3. Mời PvP (Giả lập)
    public void InvitePvP(string friendId)
    {
        Debug.Log($"[PVP] Đang gửi lời mời đến: {friendId}...");
        // Thực tế bạn sẽ bắn một notification hoặc ghi vào node "invites" của người kia trên Firebase
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