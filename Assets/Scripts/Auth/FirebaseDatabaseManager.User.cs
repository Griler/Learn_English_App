using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

public partial class FirebaseDatabaseManager : MonoBehaviour
{
    public UserProfileSO userProfileSO;
    // 1. Biến cờ và Sự kiện
    // Hàm lắng nghe data
    public void ListenToUserInfo()
    {
        // 4. KIỂM TRA AN TOÀN TUYỆT ĐỐI
        // Nếu hàm này bị gọi sớm quá khi chưa init xong -> return luôn để tránh lỗi
        if (!IsReady || dbReference == null || currentUser == null) 
        {
            Debug.LogWarning("⚠️ Gọi ListenToUserInfo quá sớm! Đang chờ Init...");
            return; 
        }

        Debug.Log("🎧 Bắt đầu lắng nghe UserInfo...");
        dbReference.Child("users").Child(currentUser.UserId).Child("userInfo").ValueChanged += HandleUserInfoChanged;
    }
    
    // Tách logic xử lý ra hàm riêng cho gọn
    private void HandleUserInfoChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null || !args.Snapshot.Exists) return;

        string json = args.Snapshot.GetRawJsonValue();
        
        // Deserialize an toàn (tránh lỗi nếu json rỗng)
        if (string.IsNullOrEmpty(json)) return;
        
        UserInfoData uInfo = JsonUtility.FromJson<UserInfoData>(json);

        UnityMainThreadDispatcher.Instance.Enqueue(() => {
             // Giả sử bạn có biến userProfileSO ở đây hoặc truyền vào
             userProfileSO.UpdateUserInfo(uInfo); 
             Debug.Log("Updated User Info from Firebase");
        });
    }
    
    public async Task AddCoins(int amount)
    {
        int currentCoins = userProfileSO.userInfo.coin;
        int newTotal = currentCoins + amount;
        await dbReference.Child("users").Child(currentUser.UserId).Child("userInfo").Child("coin").SetValueAsync(newTotal);
        Debug.Log("Coins updated: " + newTotal);
    }


    // --- LUỒNG 2: FRIEND LIST ---
    public void ListenToFriends()
    {
        // Chỉ trỏ vào node "friend"
        dbReference.Child("users").Child(currentUser.UserId).Child("friend").ValueChanged += (sender, args) => 
        {
            if (args.DatabaseError != null || !args.Snapshot.Exists)
            {
                return;
            }

            List<FriendData> fList = new List<FriendData>();
            
            // Duyệt danh sách
            foreach (DataSnapshot child in args.Snapshot.Children)
            {
                FriendData friend = new FriendData();
                // Lấy data an toàn
                if(child.Child("userId").Value != null)
                    friend.userId = child.Child("userId").Value.ToString();
                    
                fList.Add(friend);
            }

            // Đẩy về SO
            UnityMainThreadDispatcher.Instance.Enqueue(() => {
                userProfileSO.UpdateFriendList();
            });
        };
    }
    public void SetUserStatus(string status)
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null && dbReference != null)
        {
            dbReference.Child("users").Child(currentUser.UserId).Child("userInfo").Child("status").SetValueAsync(status);
        }
    }
}