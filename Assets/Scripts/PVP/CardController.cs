using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DeckItem
{
    public CardDataModel data;
    public bool  isTypeWorld;

    public DeckItem(CardDataModel d, bool t)
    {
        data = d;
        isTypeWorld = t;
    }
}
public class CardController : BaseCode
{
    [Header("UI References")]
    public Image cardBackground;      // Ảnh mặt sau (Úp)
    public GameObject frontContent;   // Group mặt trước
    public Image iconImage;           // Ảnh item
    public TextMeshProUGUI wordLabel; // Chữ
    public Button cardButton;

    [Header("Data ReadOnly")]
    public int cardId;
    public int indexInGrid;
    
    // Public để debug inspector dễ hơn
    public bool isFaceUp = false;
    public bool isLocked = false;

    private CardGameController gameManager;

    public void Init(int id, int index, string text, string spriteName, CardGameController manager)
    {
        this.cardId = id;
        this.indexInGrid = index;
        this.gameManager = manager;
        this.wordLabel.text = text;
        
        // Load ảnh từ Resources
        this.iconImage.sprite = assetManager.getSpriteAnimal(spriteName);

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClick);

        ResetState();
    } 
    public void Init(int id, int index, string text, string spriteName, CardGameController manager, bool isWord)
    {
        this.cardId = id;
        this.indexInGrid = index;
        this.gameManager = manager;
        this.wordLabel.text = text;
        this.iconImage.sprite = assetManager.getSpriteAnimal(spriteName);
        this.iconImage.gameObject.SetActive(!isWord);
        this.wordLabel.gameObject.SetActive(isWord);
        // Load ảnh từ Resources

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClick);

        ResetState();
    }

    void OnCardClick()
    {
        if (isFaceUp || isLocked) return;
        gameManager.OnCardClicked(indexInGrid);
    }

    public void FlipOpen()
    {
        if (isFaceUp) return;
        isFaceUp = true;

        // --- QUAN TRỌNG: Kill tween cũ để tránh xung đột ---
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScaleX(0, 0.15f).OnComplete(() =>
        {
            cardBackground.gameObject.SetActive(false);
            frontContent.SetActive(true);
            transform.DOScaleX(1, 0.15f);
        });
    }

    public void FlipClose()
    {
        if (isLocked) return;
        isFaceUp = false;

        // --- QUAN TRỌNG: Kill tween cũ ---
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScaleX(0, 0.15f).OnComplete(() =>
        {
            cardBackground.gameObject.SetActive(true);
            frontContent.SetActive(false);
            transform.DOScaleX(1, 0.15f);
        });
    }

    public void LockCard()
    {
        isLocked = true;
        isFaceUp = true; 
        cardButton.interactable = false;

        // Hiệu ứng nảy lên báo hiệu ăn điểm
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f);
    }

    void ResetState()
    {
        isFaceUp = false;
        isLocked = false;
        cardButton.interactable = true;
        cardBackground.gameObject.SetActive(false);
        frontContent.SetActive(true);
        transform.localScale = Vector3.one;
    }
}