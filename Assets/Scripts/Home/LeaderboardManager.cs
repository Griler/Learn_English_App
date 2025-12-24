using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using TMPro; // QUAN TRỌNG: Import thư viện này

public class LeaderboardManager : MonoBehaviour
{
    public List<GameObject> rankItem;
    public TextMeshProUGUI statusText;
    void Start()
    {
        statusText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        statusText.gameObject.SetActive(false);
        GetTop5Users();
    }

    public void GetTop5Users()
    {
        DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.GetReference("users");

        // Query vẫn như cũ: trỏ vào userInfo/rankPoint
        Query query = dbRef.OrderByChild("userInfo/rankPoint").LimitToLast(5);
           query.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                List<UserData> topUsers = new List<UserData>();
                
                if (snapshot.HasChildren)
                {
                    Debug.Log(snapshot.ChildrenCount);
                    foreach (DataSnapshot userSnap in snapshot.Children)
                    {
                        // 1. Lấy chuỗi JSON thô từ Firebase
                        string jsonRaw = userSnap.GetRawJsonValue();
                        UserData userObj = JsonConvert.DeserializeObject<UserData>(jsonRaw);
                        Debug.Log(userObj.UserInfo.name + "|" + userObj.UserInfo.rankPoint);
                        if (userObj != null)
                        {
                            // 3. Gán UserID (Key) thủ công vì Key không nằm trong chuỗi JSON
                            userObj.UserId = userSnap.Key;
                            
                            topUsers.Add(userObj);
                        }
                    }

                    // 4. Đảo ngược list (vì Firebase trả về Tăng Dần: Thấp -> Cao)
                    topUsers.Reverse();

                    // 5. Hiển thị
                    ShowLeaderboard(topUsers);
                }
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Lỗi: " + task.Exception);
            }
        });
    }

    private void ShowLeaderboard(List<UserData> users)
    {
        Debug.Log("=== BẢNG XẾP HẠNG (NEWTONSOFT) ===");
        int rank = 1;
        for (int i = 0; i < 5; i++)
        {
            rankItem[i].GetComponent<RankItem>().setData(users[i].UserInfo);
        }
        foreach (var user in users)
        {
            // Kiểm tra null để tránh lỗi nếu data thiếu
            if (user.UserInfo != null)
            {
                Debug.Log($"Top {rank}: {user.UserInfo.name} - Rank: {user.UserInfo.rankPoint}");
                
            }
            else
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "Lỗi dữ liệu".ToUpper();
                Debug.Log($"Top {rank}: {user.UserId} (Lỗi data userInfo)");
            }
            rank++;
        }
    }
}
[Serializable]
public class UserData
{
    // Biến này để hứng cái Key (UserID), ta sẽ gán thủ công vì nó không nằm trong JSON
    [JsonIgnore] 
    public string UserId { get; set; }

    [JsonProperty("userInfo")]
    public UserInfoData UserInfo { get; set; }
}