using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Cần cho ISelectHandler
using DG.Tweening;
using TMPro; // Cần DOTween

public class KeyboardSimple : MonoBehaviour, ISelectHandler
{
    [Header("UI References")]
    public RectTransform keyboardPanel;
    public TMP_InputField inputField; // Kéo InputField vào hoặc để code tự tìm

    [Header("Animation Settings")]
    public float duration = 0.3f;
    public float hiddenY = -800f; // Vị trí ẩn
    public float shownY = 0f;     // Vị trí hiện

    void Start()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();

        // Mặc định ẩn bàn phím ngay khi vào game
        if (keyboardPanel != null)
        {
            keyboardPanel.anchoredPosition = new Vector2(keyboardPanel.anchoredPosition.x, hiddenY);
        }
    }

    // 1. Chỉ hiện khi bấm vào InputField
    public void OnSelect(BaseEventData eventData)
    {
        keyboardPanel.DOKill();
        keyboardPanel.DOAnchorPosY(shownY, duration).SetEase(Ease.OutBack);
        GameManager.Instance.setInputField(inputField);
    }

    // 2. Hàm này để gọi thủ công (gắn vào nút Enter/Done/Close)
    public void HideKeyboard()
    {
        keyboardPanel.DOKill();
        keyboardPanel.DOAnchorPosY(hiddenY, duration).SetEase(Ease.InBack);
        
        // Bỏ focus khỏi InputField để tắt nhấp nháy con trỏ (tuỳ chọn)
        EventSystem.current.SetSelectedGameObject(null);
    }
}