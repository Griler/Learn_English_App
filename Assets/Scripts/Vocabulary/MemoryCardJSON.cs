using UnityEngine;
using UnityEngine.UI;

public class MemoryCardJson : MonoBehaviour
{
    public Image cardImage;
    public Button cardButton;
    public Sprite backSprite;
    
    [SerializeField] public AnimalData animalData;
    [HideInInspector] public bool isRevealed = false;
    [SerializeField] public int pairId;
    
    public void Setup(AnimalData data, Sprite back)
    {
        animalData = data;
        backSprite = back;
        cardImage.sprite = backSprite;
        
        cardButton.onClick.AddListener(OnCardClicked);
    }
    
    public void Reveal()
    {
        isRevealed = true;
        cardImage.sprite = animalData.sprite;
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