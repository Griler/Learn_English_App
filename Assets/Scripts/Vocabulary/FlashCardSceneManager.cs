using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlashCardSceneManager : MonoBehaviour
{
    private AnimalCategorySO animalCategorySo;
    private string pathLoad = $" {GlobalData.pathData}/{GlobalData.pathAnimalData}/{GlobalData.selectedNameSO}";
    private List<AnimalData> listAnimals = new List<AnimalData>();
    [SerializeField] private int currentAnimal = 0;
    [SerializeField] private GameObject cardItem;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button preButton;
    [SerializeField] private Button backButton;
    [SerializeField] private QuizManager quizManager; 
    private CardItem cardItemCmp;
    void Start()
    {
        nextButton.onClick.AddListener(btnNextClicked);
        preButton.onClick.AddListener(btnPrevClicked);
        backButton.onClick.AddListener(btnBackClicked);
        setUpData();
    }

    private void setUpData()
    {
        string topic = PlayerPrefs.GetString("SelectedMainTopic");
        string subTopic = PlayerPrefs.GetString("SelectedSubTopic");
        FirebaseDatabaseManager.Instance.LoadWords(topic,subTopic,OnWordsLoaded);
    }

    void OnWordsLoaded(List<AnimalData> words)
    {
        if (words == null)
        {
            Debug.LogError("Không tải được dữ liệu!");
            return;
        }

        Debug.Log("✅ Đã load " + words.Count + " từ!");
        foreach (var w in words)
        {
            Debug.Log($"{w.name_en} - {w.name_vi}");
        }
        listAnimals.AddRange(words);
        quizManager.initQuiz(listAnimals);
        updateCard(currentAnimal);
    }
    
    private void updateCard(int currentAnimal = 0)
    {
        cardItemCmp = cardItem.GetComponent<CardItem>();
        cardItemCmp.setUpCard(listAnimals[currentAnimal]);
    }

    void btnNextClicked()
    {
        if (currentAnimal < listAnimals.Count - 1 )
        {
            currentAnimal++;
            updateCard(currentAnimal);
        }
    }

    void btnPrevClicked()
    {
        if (currentAnimal > 0)
        {
            currentAnimal--;
            updateCard(currentAnimal);
        }
    }

    void btnBackClicked()
    {
        SceneManager.LoadSceneAsync("HomeScene");
    }
}
