using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI opponentText;
    public TextMeshProUGUI rankChangeText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI matchId;
    
    public void SetData(MatchHistoryData data)
    {
        dateText.text = "Date: " + data.matchDate;
        opponentText.text = "VS: " + data.opponentName;
        modeText.text = "Mode: " + data.mode;
        if (data.matchId is not null)
        {
            if (data.matchId != "")
            {
                string showId = data.matchId.Substring(0, 7) + "..." + data.matchId.Substring(data.matchId.Length - 4);
                matchId.text = "Match id: " + showId;
            }
        }
        else
        {
            matchId.text = "";
        }
        // Xử lý màu sắc và dấu +/- cho Rank
        if (data.rankChange >= 0)
        {
            rankChangeText.text = "Rank :+" + data.rankChange.ToString();
        }
        else
        {
            rankChangeText.text = "Rank: " + data.rankChange.ToString();
        }
    }
}