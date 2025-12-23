using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastNetwork : MonoBehaviour
{
    public TextMeshProUGUI textNotice;
    public Button againButton;
    public Action actionOnClickButton;

    public Action ActionOnClickButton
    {
        get => actionOnClickButton;
        set => actionOnClickButton = value;
    }

    public static ToastNetwork Instance;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        gameObject.GetComponentInChildren<CanvasGroup>().alpha = 0;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        againButton.onClick.AddListener(() =>
        {
            this.textNotice.text = "Đang thử lại ...";
            actionOnClickButton?.Invoke();
            againButton.gameObject.SetActive(false);
        });
    }

    public void showDisconnect(string text = "Có lỗi xảy ra vui lòng thử lại ....")
    {
        againButton.gameObject.SetActive(true);
        this.textNotice.text = text;
        gameObject.SetActive(true);
        gameObject.GetComponentInChildren<CanvasGroup>().alpha = 1;
    }

    public void hideDisconnect()
    {
        gameObject.GetComponentInChildren<CanvasGroup>().alpha = 0;
        gameObject.SetActive(false);
    }

    public void setAction(Action action)
    {
        actionOnClickButton = action;
    }
}
