using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MemoryMatchGameJSON : MonoBehaviour
{
    [Header("Setup")]
    public GameObject cardPrefab;
    public Transform cardContainer;
    public Sprite cardBackSprite;
    
    [Header("UI")]
    public TextMeshProUGUI movesText;
    public TextMeshProUGUI pairsText;
    public TextMeshProUGUI timerText;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip matchSound;
    public AudioClip wrongSound;
    
    private List<MemoryCardJson> cards = new List<MemoryCardJson>();
    private List<GameObject> cardPool = new List<GameObject>(); // 🔹 Pool lưu các card đã tạo

    private MemoryCardJson firstCard, secondCard;
    private int moves = 0;
    private int pairsFound = 0;
    private int totalPairs = 6;
    private float gameTime = 0f;
    private bool gameActive = true;
    
    void Start()
    {
        GenerateCards();
    }
    
    void Update()
    {
        if (gameActive)
        {
            gameTime += Time.deltaTime;
            UpdateTimer();
        }
    }
    
    void GenerateCards()
    {
        // 1️⃣ Lấy category
        string category = PlayerPrefs.GetString("SelectedCategory", "All");

        List<AnimalData> animals;
        if (category == "All")
        {
            animals = VocabularyDataManager.Instance.GetRandomAnimals(totalPairs);
        }
        else
        {
            animals = VocabularyDataManager.Instance.GetRandomAnimalsFromCategory(category, totalPairs);
        }

        // Tạo danh sách tất cả card (mỗi con 2 cái)
        List<AnimalData> allCards = new List<AnimalData>();
        allCards.AddRange(animals);
        allCards.AddRange(animals);

        // Shuffle
        for (int i = 0; i < allCards.Count; i++)
        {
            var temp = allCards[i];
            int rand = UnityEngine.Random.Range(i, allCards.Count);
            allCards[i] = allCards[rand];
            allCards[rand] = temp;
        }

        var existingCards = new List<MemoryCardJson>(cardContainer.GetComponentsInChildren<MemoryCardJson>(true));
        foreach (var card in existingCards)
            card.gameObject.SetActive(false);

        cards.Clear();
        for (int i = 0; i < allCards.Count; i++)
        {
            MemoryCardJson card;

            if (i < existingCards.Count)
            {
                // Dùng card có sẵn trong container
                card = existingCards[i];
            }
            else
            {
                // Không đủ, sinh thêm card mới
                var cardObj = Instantiate(cardPrefab, cardContainer);
                card = cardObj.GetComponent<MemoryCardJson>();
                existingCards.Add(card);
            }
            card.gameObject.SetActive(true);
            card.Setup(allCards[i], cardBackSprite);
            cards.Add(card);
        }

        // Ẩn các card dư (nếu có)
        for (int i = allCards.Count; i < existingCards.Count; i++)
            existingCards[i].gameObject.SetActive(false);

        UpdateUI();
    }


    
    public void CardClicked(MemoryCardJson card)
    {
        if (firstCard == null)
        {
            firstCard = card;
            firstCard.Reveal();
        }
        else if (secondCard == null && card != firstCard)
        {
            secondCard = card;
            secondCard.Reveal();
            moves++;
            
            Invoke("CheckMatch", 1f);
        }
        
        UpdateUI();
    }
    
    void CheckMatch()
    {
        if (firstCard.animalData.id == secondCard.animalData.id)
        {
            // Match found
            pairsFound++;
            
            // Play match sound
            if (audioSource && matchSound)
                audioSource.PlayOneShot(matchSound);
            
            // Play animal sound
            if (firstCard.animalData.audioClip)
                audioSource.PlayOneShot(firstCard.animalData.audioClip);
            
            if (pairsFound >= totalPairs)
            {
                gameActive = false;
                Invoke("EndGame", 1.5f);
            }
        }
        else
        {
            // No match
            firstCard.Hide();
            secondCard.Hide();
            
            if (audioSource && wrongSound)
                audioSource.PlayOneShot(wrongSound);
        }
        
        firstCard = null;
        secondCard = null;
        UpdateUI();
    }
    
    void UpdateUI()
    {
        movesText.text = $"Moves: {moves}";
        pairsText.text = $"Pairs: {pairsFound}/{totalPairs}";
    }
    
    void UpdateTimer()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        timerText.text = $"Time: {minutes:00}:{seconds:00}";
    }
    
    void EndGame()
    {
        int stars = 3;
        if (moves > totalPairs * 2) stars = 2;
        if (moves > totalPairs * 3) stars = 1;
        
        Debug.Log($"Memory Game Complete! Moves: {moves}, Time: {gameTime:F1}s, Stars: {stars}");
        
        // Save progress and return to menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
