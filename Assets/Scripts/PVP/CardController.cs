using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Dùng để làm hiệu ứng lật

public class CardController : BaseCode
{
    [Header("UI References")]
    public Image cardBackground;      // Ảnh nền thẻ (Mặt sau/Mặt úp)
    public GameObject frontContent;   // Group chứa Icon và Chữ (Mặt trước)
    public Image iconImage;           // Ảnh hiển thị (Táo, Chuối...)
    public TextMeshProUGUI wordLabel; // Chữ tiếng Anh
    public Button cardButton;         // Component Button để click

    [Header("Data (Read Only)")]
    public int cardId;        // ID loại thẻ (để so sánh giống nhau)
    public int indexInGrid;   // Vị trí trên bàn cờ (để gửi qua mạng)
    
    private CardGameController gameManager;
    private bool isFaceUp = false;
    private bool isLocked = false;

    // Hàm khởi tạo - Được gọi từ CardGameController khi sinh thẻ
    public void Init(int id, int index, string text, string spriteName, CardGameController manager)
    {
        this.cardId = id;
        this.indexInGrid = index;
        this.gameManager = manager;
        
        // 1. Setup UI
        this.wordLabel.text = text;
        
        // 2. Load ảnh từ thư mục Resources
        // Lưu ý: Ảnh phải nằm trong thư mục: Assets/Resources/IconCards/ (hoặc đường dẫn tương ứng)
        Sprite loadedSprite = assetManager.getSpriteAnimal(spriteName);
        if (loadedSprite != null)
        {
            this.iconImage.sprite = loadedSprite;
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy ảnh: {spriteName} trong Resources!");
        }

        // 3. Setup Button
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClick);

        // 4. Mặc định úp thẻ
        ResetState();
    }

    // Sự kiện khi người chơi bấm vào thẻ
    void OnCardClick()
    {
        // Nếu thẻ đã lật, hoặc đã bị ăn, hoặc game đang bận -> Không làm gì
        if (isFaceUp || isLocked) return;

        // Gửi yêu cầu lên Manager (Manager sẽ check lượt và gửi RPC lật thẻ)
        gameManager.OnCardClicked(indexInGrid);
    }

    // --- CÁC HÀM VISUAL (HIỂU ỨNG) ---

    // 1. Lật ngửa thẻ (Mở ra)
    public void FlipOpen()
    {
        if (isFaceUp) return;
        isFaceUp = true;

        // Hiệu ứng lật: Co lại chiều X -> Đổi hình -> Mở ra chiều X
        transform.DOScaleX(0, 0.15f).OnComplete(() =>
        {
            cardBackground.gameObject.SetActive(false); // Tắt mặt sau
            frontContent.SetActive(true);               // Hiện mặt trước
            
            transform.DOScaleX(1, 0.15f);
        });
    }

    // 2. Úp thẻ lại (Khi chọn sai)
    public void FlipClose()
    {
        if (!isFaceUp || isLocked) return; // Nếu đã khóa thì không úp lại
        isFaceUp = false;

        transform.DOScaleX(0, 0.15f).OnComplete(() =>
        {
            cardBackground.gameObject.SetActive(true);  // Hiện mặt sau
            frontContent.SetActive(false);              // Tắt mặt trước
            
            transform.DOScaleX(1, 0.15f);
        });
    }

    // 3. Khóa thẻ (Khi chọn đúng cặp)
    public void LockCard()
    {
        isLocked = true;
        isFaceUp = true;
        cardButton.interactable = false; // Không cho click nữa

        // Hiệu ứng báo hiệu đã ăn (Ví dụ: Nháy màu xanh hoặc mờ đi chút)
        // Cách 1: Làm mờ đi
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.DOFade(0.5f, 0.3f);
        
        // Cách 2: Phóng to nhẹ rồi về chỗ cũ (nảy lên)
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f);
    }

    void ResetState()
    {
        isFaceUp = false;
        isLocked = false;
        cardButton.interactable = true;
        
        cardBackground.gameObject.SetActive(true);
        frontContent.SetActive(false);
        transform.localScale = Vector3.one; // Reset scale phòng trường hợp lỗi tween
    }
}