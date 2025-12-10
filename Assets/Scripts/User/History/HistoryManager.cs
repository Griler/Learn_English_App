using UnityEngine;
using Firebase.Database; // Nhớ import thư viện Firebase
using Firebase.Extensions; // Để dùng ContinueWithOnMainThread
using System;
using System.Collections.Generic;

public class HistoryManager : MonoBehaviour
{
    DatabaseReference reference;
    string currentUserId; // ID của người chơi hiện tại

    void Start()
    {
        // Khởi tạo reference tới database
        reference = FirebaseDatabase.DefaultInstance.RootReference;
        
        // GIẢ LẬP: Lấy UserID (thực tế bạn lấy từ Firebase Auth)
        currentUserId = "user_12345"; 
    }

    // --- HÀM 1: LƯU LỊCH SỬ ĐẤU ---
    public void SaveMatchHistory(string opponentName, string result, int rankChange, string gameMode, string matchId)
    {
        // Lấy thời gian hiện tại
        string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Tạo object dữ liệu
        MatchHistoryData newData = new MatchHistoryData(matchId, opponentName, currentDateTime, result, rankChange, gameMode);

        // Chuyển đổi sang JSON
        string json = JsonUtility.ToJson(newData);

        // Đường dẫn: user/userid/history/matchId
        // Dùng matchId làm key con để dễ tìm kiếm, hoặc dùng Push() để tạo key ngẫu nhiên
        reference.Child("user").Child(currentUserId).Child("history").Child(matchId).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Lưu lịch sử đấu thành công!");
                }
                else
                {
                    Debug.LogError("Lưu thất bại: " + task.Exception);
                }
            });
    }

    // --- HÀM 2: LẤY LỊCH SỬ CHO POPUP ---
    // Callback action để trả dữ liệu về UI sau khi tải xong
    public void LoadHistory(Action<List<MatchHistoryData>> onLoaded) 
    {
        reference.Child("user").Child(currentUserId).Child("history")
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
    public string matchId;        // ID trận đấu
    public string opponentName;   // Tên đối thủ
    public string dateTime;       // Ngày giờ đấu (dạng string cho dễ lưu)
    public string result;         // Kết quả: "Win", "Lose", "Draw"
    public int rankChange;        // Điểm rank thay đổi (ví dụ: +20 hoặc -15)
    public string gameMode;       // Chế độ: "PvP", "Solo", "Ranked"

    // Constructor rỗng bắt buộc cho Firebase
    public MatchHistoryData() { }

    // Constructor tiện lợi để tạo dữ liệu nhanh
    public MatchHistoryData(string id, string oppName, string time, string res, int rank, string mode)
    {
        matchId = id;
        opponentName = oppName;
        dateTime = time;
        result = res;
        rankChange = rank;
        gameMode = mode;
    }
}