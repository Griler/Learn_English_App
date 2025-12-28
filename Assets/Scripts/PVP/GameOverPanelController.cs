using System;
using UnityEngine;
using TMPro;
using System.Collections;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverPanelController : MonoBehaviour
{
    [Header("--- UI REFERENCES ---")]
    [Tooltip("Text hiển thị: Chiến Thắng / Thất Bại")]
    public TextMeshProUGUI resultTitleText;

    [Tooltip("Text hiển thị số điểm Rank thay đổi (VD: +25)")]
    public TextMeshProUGUI rankChangeText;

    [Tooltip("Text đếm ngược tự động thoát (VD: Về trang chủ sau 3s)")]
    public TextMeshProUGUI countdownText;

    public Button homeButton;
    public Button againButton;
    
    private int modegame = 0;

    public int Modegame
    {
        get => modegame;
        set => modegame = value;
    }

    private void Start()
    {
        homeButton.onClick.AddListener(() =>
        {
            LeaveRoom();
        });
        againButton.onClick.AddListener((() =>
        {
            GlobalData.AutoJoinMode = modegame;
            LeaveRoom();
        }));
    }

    public void ShowGameOver(int rankPointChange)
    {
        againButton.gameObject.SetActive(NetworkGameState.CurrentJoinType != NetworkGameState.JoinType.FriendInvite);
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
        string textNotice = countdownText.text;
        for (int i = 5; i >= 0; i--)
        {
            countdownText.text = $"{textNotice}\n Về trang chủ sau: {i}s";
            if (i == 1)
            {
                homeButton.interactable = false;
                againButton.interactable = false;
            }
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
            SceneManager.LoadScene("HomeScene");
        }
    }
    
    private string myPlayerName()
    {
        return PhotonNetwork.NickName;
    }
}