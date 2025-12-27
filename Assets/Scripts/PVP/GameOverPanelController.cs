using UnityEngine;
using TMPro;
using System.Collections;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameOverPanelController : MonoBehaviour
{
    [Header("--- UI REFERENCES ---")]
    [Tooltip("Text hiển thị: Chiến Thắng / Thất Bại")]
    public TextMeshProUGUI resultTitleText;

    [Tooltip("Text hiển thị số điểm Rank thay đổi (VD: +25)")]
    public TextMeshProUGUI rankChangeText;

    [Tooltip("Text đếm ngược tự động thoát (VD: Về trang chủ sau 3s)")]
    public TextMeshProUGUI countdownText;

    public void ShowGameOver(int rankPointChange)
    {
        if (rankPointChange >= 0)
        {
            rankChangeText.text = $"+{rankPointChange}";
        }
        else
        {
            rankChangeText.text = $"{rankPointChange}";
        }

        // 3. Bắt đầu đếm ngược
        StartCoroutine(CountdownRoutine());
    }



    IEnumerator CountdownRoutine()
    {
        for (int i = 5; i >= 0; i--)
        {
            countdownText.text = $"Về trang chủ sau: {i}s";
            yield return new WaitForSeconds(1f);
        }
        
        // Tự động thoát phòng khi hết giờ
        LeaveRoom();
    }

    void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.ONLINE);
            LobbyController.AutoJoinMode = 1;
            SceneManager.LoadScene("HomeScene");
        }
    }

    private string myPlayerName()
    {
        return PhotonNetwork.NickName;
    }
}