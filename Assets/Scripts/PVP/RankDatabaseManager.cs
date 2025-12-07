using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions; // Nhớ dòng này để dùng ContinueWithOnMainThread
using System.Threading.Tasks;

public class RankDatabaseManager : MonoBehaviour
{
    public static RankDatabaseManager Instance;
    private DatabaseReference _dbReference;

    private void Awake()
    {
        // Tạo Singleton để dễ gọi
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Lấy tham chiếu gốc của Database
        _dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    /// <summary>
    /// Hàm này xử lý việc lưu lịch sử đấu + cập nhật tổng điểm rank
    /// </summary>
    /// 
    public async void SaveMatchHistory(string matchResult, int pointsChanged, string enemyName)
    {
        // 1. Lấy User ID hiện tại
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("Chưa đăng nhập, không thể lưu rank!");
            return;
        }
        string userId = currentUser.UserId;

        // 2. Lấy điểm Rank hiện tại từ server về trước (để cộng dồn)
        // Lưu ý: Đây là xử lý bất đồng bộ (Async)
        var snapshot = await _dbReference.Child("users").Child(userId).Child("totalRank").GetValueAsync();
        
        int currentTotalRank = 0;
        if (snapshot.Exists && snapshot.Value != null)
        {
            currentTotalRank = int.Parse(snapshot.Value.ToString());
        }

        // 3. Tính toán điểm mới
        int newTotalRank = currentTotalRank + pointsChanged;
        // Đảm bảo không bị âm điểm nếu muốn
        if (newTotalRank < 0) newTotalRank = 0; 

        // 4. Tạo Object dữ liệu lịch sử
        MatchResultData historyData = new MatchResultData(matchResult, pointsChanged, newTotalRank, enemyName);
        string jsonHistory = JsonUtility.ToJson(historyData);

        // 5. Thực hiện lưu vào Database (2 việc cùng lúc)
        
        // Việc A: Cập nhật tổng điểm rank mới vào node "totalRank"
        Task updateRankTask = _dbReference.Child("users").Child(userId)
            .Child("userInfo").Child("rankPoint").SetValueAsync(newTotalRank);

        // Việc B: Thêm lịch sử đấu vào node "history". 
        // Dùng .Push() để tạo ra một key ngẫu nhiên (dạng list)
        Task updateHistoryTask = _dbReference.Child("users").Child(userId).Child("history").Push().SetRawJsonValueAsync(jsonHistory);

        // Chờ cả 2 việc xong
        await Task.WhenAll(updateRankTask, updateHistoryTask);

        Debug.Log($"Đã lưu xong! Rank mới: {newTotalRank}. Kết quả: {historyData.result}");
    }
}