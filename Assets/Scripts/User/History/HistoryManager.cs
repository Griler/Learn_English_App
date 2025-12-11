using UnityEngine;
using Firebase.Database; // Nhớ import thư viện Firebase
using Firebase.Extensions; // Để dùng ContinueWithOnMainThread
using System;
using System.Collections.Generic;

public class HistoryManager : MonoBehaviour
{
    DatabaseReference reference;
    string currentUserId; // ID của người chơi hiện tại
    
    public void LoadHistory(Action<List<MatchHistoryData>> onLoaded, Action onError) 
    {
        reference = FirebaseDatabaseManager.Instance.dbReference;
        currentUserId = FirebaseDatabaseManager.Instance.currentUser.UserId; 
        reference.Child("users").Child(currentUserId).Child("history")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Lỗi tải lịch sử");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                List<MatchHistoryData> historyList = new List<MatchHistoryData>();

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        string json = child.GetRawJsonValue();
                        MatchHistoryData matchData = JsonUtility.FromJson<MatchHistoryData>(json);
                        historyList.Add(matchData);
                    }
                    
                    // Đảo ngược danh sách để trận mới nhất lên đầu (tùy chọn)
                    historyList.Reverse(); 
                }

                // Trả dữ liệu về cho UI xử lý
                onLoaded?.Invoke(historyList);
            });
    }
}

[Serializable]
public class MatchHistoryData
{
    public string matchDate;
    public string result;      // "Win" hoặc "Lose"
    public int rankChange;     // Ví dụ: +25 hoặc -15
    public int currentRank;    // Điểm rank sau khi cộng trừ
    public string opponentName; // Tên đối thủ (nếu cần)
    public string mode;     // Chế độ: "PvP", "Solo", "Ranked"
    public string matchId;

    // Constructor rỗng bắt buộc cho Firebase
    public MatchHistoryData() { }

    // Constructor tiện lợi để tạo dữ liệu nhanh
    public MatchHistoryData(string id, string oppName, string time, string res, int rank, string mode)
    {
        
    }
}