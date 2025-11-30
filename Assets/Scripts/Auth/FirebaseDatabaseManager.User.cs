using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public partial class FirebaseDatabaseManager : MonoBehaviour
{
    public UserProfileSO userProfileSO;
    
    public void ListenToUserInfo()
    {
        // Chỉ trỏ vào node "userInfo"
        dbRef.Child("users").Child(currentUser.UserId).Child("userInfo").ValueChanged += (sender, args) => 
        {
            if (args.DatabaseError != null || !args.Snapshot.Exists) return;

            string json = args.Snapshot.GetRawJsonValue();
            UserInfoData uInfo = JsonUtility.FromJson<UserInfoData>(json);

            // Đẩy về SO
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                userProfileSO.UpdateUserInfo(uInfo);
            });
        };
    }
    
    public async Task AddCoins(int amount)
    {
        int currentCoins = userProfileSO.userInfo.coin;
        int newTotal = currentCoins + amount;
        await dbReference.Child("users").Child(currentUser.UserId).Child("userInfo").Child("coin").SetValueAsync(newTotal);
        Debug.Log("Coins updated: " + newTotal);
    }


    // --- LUỒNG 2: FRIEND LIST ---
    void ListenToFriends()
    {
        // Chỉ trỏ vào node "friend"
        dbRef.Child("users").Child(currentUser.UserId).Child("friend").ValueChanged += (sender, args) => 
        {
            if (args.DatabaseError != null || !args.Snapshot.Exists) return;

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
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                userProfileSO.UpdateFriendList(fList);
            });
        };
    }
}