using System;

[Serializable] // Bắt buộc để Firebase hoặc JsonUtility hiểu được
public class MatchResultData
{
    public string matchDate;
    public string result;      // "Win" hoặc "Lose"
    public int rankChange;     // Ví dụ: +25 hoặc -15
    public int currentRank;    // Điểm rank sau khi cộng trừ
    public string opponentName; // Tên đối thủ (nếu cần)

    // Hàm khởi tạo cho tiện
    public MatchResultData(bool isWin, int pointsChanged, int newTotalRank, string enemyName)
    {
        // Lưu thời gian hiện tại
        this.matchDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.result = isWin ? "Win" : "Lose";
        this.rankChange = pointsChanged; // Nếu thua thì truyền số âm vào
        this.currentRank = newTotalRank;
        this.opponentName = enemyName;
    }
}