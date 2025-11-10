using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject flashcardPanel;
    public GameObject quizPanel;
    private List<AnimalData> listAnimals = new List<AnimalData>();
    [SerializeField] private int currentAnimalIndex = 0;
    [SerializeField] private int currentQuiz = 0;
    [SerializeField] private AnimalData currentAnimalData;
    [SerializeField] private Button backButton;

    private void Start()
    {
        backButton.onClick.AddListener(onClickBackButton);
        setUpData();
    }
    
    public void ShowFlashcard()
    {   
        flashcardPanel.SetActive(true);
        currentAnimalData = listAnimals[0];
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateCard(currentAnimalData);
        quizPanel.SetActive(false);
    }

    public void ShowFlashcardByButton()
    {
        if (currentAnimalIndex >= listAnimals.Count - 1 )
        {
            ShowFinishPanel();
            return;
        }    
        bool isCorrect = quizPanel.GetComponent<QuizManager>().getCorrectAnswer();
        if (!isCorrect) return;
        StartCoroutine(showNextFlashCard());
    }
    IEnumerator  showNextFlashCard() 
    {
        yield return new WaitForSeconds(0.75f);
        flashcardPanel.SetActive(true);
        currentAnimalData = listAnimals[currentAnimalIndex];
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateCard(currentAnimalData);
        quizPanel.SetActive(false);
    }


    public void ShowQuiz()
    {
        flashcardPanel.SetActive(false);
        if (currentAnimalIndex < listAnimals.Count - 1 )
        {
            currentAnimalIndex++;
        }      
        quizPanel.GetComponent<QuizManager>().UpdateUI(currentAnimalData);
        quizPanel.SetActive(true);
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
        quizPanel.GetComponent<QuizManager>().initQuiz(listAnimals);
        ShowFlashcard();
    }
    
    void onClickBackButton()
    {
        SceneManager.LoadSceneAsync("HomeScene");
    }
    
    void ShowFinishPanel()
    {
        GameEvents.ShowNotifcation("Bạn đã hoàn thành khoá học. Sẽ Trở Về Trang Chủ",Color.black);
    }
}
