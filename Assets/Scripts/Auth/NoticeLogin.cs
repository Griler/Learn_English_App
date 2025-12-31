using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoticeLogin : MonoBehaviour
{
    public TextMeshProUGUI textNotice;
    public Button confirmButton;
    public GameObject container;
    public Button sendMailButton;
    

    public void showNotice(string msg, Action callback = null)
    {
        sendMailButton.gameObject.SetActive(false);
        textNotice.text = msg;
        container.SetActive(true);
        confirmButton.gameObject.SetActive(callback != null);
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener((() =>
        {
            callback?.Invoke();
            confirmButton.onClick.RemoveAllListeners();
        }));
    }
}
