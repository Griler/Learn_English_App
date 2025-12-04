using UnityEngine;
using TMPro;
using DG.Tweening; // Bắt buộc

public class ToastSystem : MonoBehaviour
{
    public static ToastSystem Instance; // Singleton để gọi từ đâu cũng được

    [Header("UI References")]
    public CanvasGroup toastCanvasGroup; // Kéo object có CanvasGroup vào đây
    public TextMeshProUGUI toastText;    // Kéo Text vào đây
    public RectTransform toastRect;      // Kéo RectTransform của ToastPanel vào (để làm hiệu ứng bay lên)

    [Header("Settings")]
    public float fadeDuration = 0.5f;    // Thời gian hiện/ẩn
    public float stayTime = 1.0f;        // Thời gian tồn tại
    public float moveDistance = 50f;     // Khoảng cách bay lên

    private Vector2 originalPos;         // Lưu vị trí gốc

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 1. Lưu vị trí ban đầu
        if (toastRect) originalPos = toastRect.anchoredPosition;

        // 2. Ẩn ngay khi vào game
        toastCanvasGroup.alpha = 0;
        toastCanvasGroup.blocksRaycasts = false;
        DOVirtual.DelayedCall(3, () =>
        {
            ShowToast("DSDSD");
        }); // Để chuột bấm xuyên qua được
    }

    // --- HÀM GỌI TOAST ---
    public void ShowToast(string message)
    {
        // 1. Cập nhật nội dung
        toastText.text = message;

        // 2. Reset trạng thái cũ (Đề phòng đang chạy dở cái cũ thì có cái mới đè lên)
        toastCanvasGroup.DOKill();
        toastRect.DOKill();

        // Đặt lại vị trí thấp hơn 1 chút để lát nó bay lên
        toastRect.anchoredPosition = originalPos + new Vector2(0, moveDistance);
        toastCanvasGroup.alpha = 0;

        // 3. TẠO CHUỖI HIỆU ỨNG (SEQUENCE)
        Sequence mySequence = DOTween.Sequence();

        // Giai đoạn 1: Hiện lên (Fade In + Bay lên vị trí gốc)
        mySequence.Append(toastCanvasGroup.DOFade(1, fadeDuration));
        mySequence.Join(toastRect.DOAnchorPos(originalPos, fadeDuration).SetEase(Ease.OutBack));

        // Giai đoạn 2: Giữ nguyên (Wait)
        mySequence.AppendInterval(stayTime);

        // Giai đoạn 3: Biến mất (Fade Out + Bay lên tiếp 1 đoạn nhỏ cho đẹp)
        mySequence.Append(toastCanvasGroup.DOFade(0, fadeDuration));
        mySequence.Join(toastRect.DOAnchorPos(originalPos + new Vector2(0, moveDistance), fadeDuration));

        // Callback tùy chọn
        mySequence.OnComplete(() => {
            Debug.Log("Toast đã tắt!");
        });
    }
}