using UnityEngine;
using TMPro;
using DG.Tweening; // Nhớ import DOTween

public class NotificationManager : BaseCode
{
    [Header("UI References")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    protected CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    protected virtual void Awake()
    {
        canvasGroup = notificationPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError($"{name}: Missing CanvasGroup on NotificationPanel!");
            return;
        }

        canvasGroup.alpha = 0;
        notificationPanel.SetActive(false);
    }

    
    public void ShowNotification(string message, Color32? color = null)
    {
        // Hủy các tween cũ đang chạy trên CanvasGroup để tránh xung đột
        canvasGroup.DOKill();

        // Cập nhật text và kích hoạt panel
        if (message != "")
        {
            notificationText.text = message;
            notificationText.color = color ?? new Color32(255, 255, 255, 255);
        }
        notificationPanel.SetActive(true);
        // Chạy animation fade in
        canvasGroup.DOFade(1f, fadeInDuration);
    }


    public void HideNotification()
    {
        canvasGroup.DOKill();

        canvasGroup.DOFade(0f, fadeOutDuration)
            .OnComplete(() => {
                notificationPanel.SetActive(false);
            });
    }
}