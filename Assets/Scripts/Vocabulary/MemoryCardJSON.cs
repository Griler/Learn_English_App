using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MemoryCardJson : MonoBehaviour
{
    public Image cardImage;
    public Button cardButton;
    public Sprite backSprite;
    public TextMeshProUGUI nameCard;
    
    [FormerlySerializedAs("animalData")] [SerializeField] public WordData wordData;
    [HideInInspector] public bool isRevealed = false;
    [SerializeField] public int pairId;
    
    public void Setup(WordData data, Sprite back)
    {
        wordData = data;
        backSprite = back;
        cardImage.sprite = backSprite;
        
        cardButton.onClick.AddListener(OnCardClicked);
    }
    
    public void Reveal()
    {
        isRevealed = true;
        cardImage.sprite = wordData.sprite;
    }
    
    public void Hide()
    {
        isRevealed = false;
        cardImage.sprite = backSprite;
    }
    
    void OnCardClicked()
    {
        if (!isRevealed)
            FindAnyObjectByType<MemoryMatchGameJSON>().CardClicked(this);
    }
}