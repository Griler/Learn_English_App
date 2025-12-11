using System;

[Serializable] // Bắt buộc để Firebase hoặc JsonUtility hiểu được
public class MatchResultData
{
    public string matchDate;
    public string result;      // "Win" hoặc "Lose"
    public int rankChange;     // Ví dụ: +25 hoặc -15
    public int currentRank;    // Điểm rank sau khi cộng trừ
    public string opponentName; // Tên đối thủ (nếu cần)
    public string mode;
    public string matchId;
    // Hàm khởi tạo cho tiện
    public MatchResultData(string matchid, string matchResult, int pointsChanged, int newTotalRank, string enemyName, string mode)
    {
        // Lưu thời gian hiện tại
        this.matchDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.result = matchResult;
        this.rankChange = pointsChanged; // Nếu thua thì truyền số âm vào
        this.currentRank = newTotalRank;
        this.opponentName = enemyName;
        this.mode = mode;
        this.matchId = matchId;
    }
}