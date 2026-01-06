using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnlinePanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Button gameButton;
    public Button historyButton;
    public Button rankingButton;

    public GameObject gameView;
    public GameObject historyView;
    public GameObject rankingView;

    public TextMeshProUGUI nameTitle;
    void Start()
    {
        if (gameButton)
            gameButton.onClick.AddListener(openGameView);
        if (historyButton)
            historyButton.onClick.AddListener(openHistoryView);
        if (rankingButton)
            rankingButton.onClick.AddListener(openRankingView);
    }

    private void OnEnable()
    {
        openGameView();
    }

    void openGameView()
    {
        gameView.SetActive(true);
        historyView.SetActive(false);
        rankingView.SetActive(false);
        nameTitle.text = "Thi Đấu".ToUpper();
    }

    void openHistoryView()
    {
        gameView.SetActive(false);
        historyView.SetActive(true);
        rankingView.SetActive(false);
        nameTitle.text = "Lịch Sử".ToUpper();

    }

    void openRankingView()
    {
        gameView.SetActive(false);
        historyView.SetActive(false);
        rankingView.SetActive(true);
        nameTitle.text = "Xếp Hạng".ToUpper();
    }

}