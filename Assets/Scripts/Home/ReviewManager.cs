using System;
using UnityEngine;
using UnityEngine.UI;

public class ReviewManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Button gameButton;
    public Button historyButton;
    public Button rankingButton;

    public GameObject gameView;
    public GameObject historyView;
    public GameObject rankingView;

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
    }

    void openHistoryView()
    {
        gameView.SetActive(false);
        historyView.SetActive(true);
        rankingView.SetActive(false);
    }

    void openRankingView()
    {
        gameView.SetActive(false);
        historyView.SetActive(false);
        rankingView.SetActive(true);
    }

}