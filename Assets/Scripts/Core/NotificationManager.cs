using UnityEngine;
using TMPro;
using DG.Tweening; // Nhớ import DOTween

public class NotificationManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    void Awake()
    {
        canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Notification Panel needs a CanvasGroup component!");
            return;
        }

        // Đảm bảo panel ẩn khi bắt đầu
        canvasGroup.alpha = 0;
        notificationPanel.SetActive(false);
    }

    /// <summary>
    /// Hiển thị thông báo và giữ nó ở đó.
    /// </summary>
    /// <param name="message">Nội dung thông báo cần hiển thị.</param>
    public void ShowNotification(string message, Color32? color = null)
    {
        // Hủy các tween cũ đang chạy trên CanvasGroup để tránh xung đột
        canvasGroup.DOKill();

        // Cập nhật text và kích hoạt panel
        notificationText.text = message;
        notificationText.color = color ?? new Color32(255, 255, 255, 255);
        notificationPanel.SetActive(true);

        // Chạy animation fade in
        canvasGroup.DOFade(1f, fadeInDuration);
    }

    /// <summary>
    /// Ẩn thông báo đang hiển thị.
    /// </summary>
    public void HideNotification()
    {
        // Hủy các tween cũ đang chạy
        canvasGroup.DOKill();

        // Chạy animation fade out và tắt panel khi hoàn thành
        canvasGroup.DOFade(0f, fadeOutDuration)
            .OnComplete(() => {
                notificationPanel.SetActive(false);
            });
    }
}