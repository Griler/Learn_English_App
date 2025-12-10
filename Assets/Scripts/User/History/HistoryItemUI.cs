using UnityEngine;
using UnityEngine.UI;

public class HistoryItemUI : MonoBehaviour
{
    public Text dateText;
    public Text opponentText;
    public Text resultText;
    public Text rankChangeText;
    public Text modeText;
    public Text matchId;
    
    public void SetData(MatchHistoryData data)
    {
        dateText.text = data.dateTime;
        opponentText.text = "VS: " + data.opponentName;
        resultText.text = data.result;
        modeText.text = data.gameMode;
        matchId.text = data.matchId;
        // Xử lý màu sắc và dấu +/- cho Rank
        if (data.rankChange >= 0)
        {
            rankChangeText.text = data.rankChange.ToString();
            rankChangeText.color = Color.green; // Xanh lá nếu cộng điểm
        }
        else
        {
            rankChangeText.text = data.rankChange.ToString();
            rankChangeText.color = Color.red; // Đỏ nếu trừ điểm
        }
    }
}