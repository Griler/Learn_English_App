using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening; // Nhớ import DOTween

public class NotificationManagerGrammar : NotificationManager
{
    [SerializeField]Button PracticeBtn;

    private void OnEnable()
    {
        GameEvents.showNotification += ShowNotification;
    } 
    private void OnDestroy()
    {
        GameEvents.showNotification -= ShowNotification;
    }

    protected override void Awake()
    {
        homeBtn.onClick.AddListener(onClickHomeBtn);
        PracticeBtn.onClick.AddListener(onClickPracticeBtn);
        base.Awake();
    }

    void onClickHomeBtn()
    {
        SceneManager.LoadSceneAsync(GlobalData.homeScene);
    }

    void onClickPracticeBtn()
    {
        HideNotification();
        GameEvents.ShowExerciseUI();
    }
    
    public void ShowNotification(string message, Color32? color = null)
    {
        canvasGroup.DOKill();

        if (message != "")
        {
            notificationText.text = message;
            notificationText.color = color ?? new Color32(255, 255, 255, 255);
        }
        notificationPanel.SetActive(true);
        canvasGroup.DOFade(1f, fadeInDuration);
    }
}
