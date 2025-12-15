using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject flashcardPanel;
    public GameObject quizPanel;
    private List<WordData> listAnimals = new List<WordData>();
    [SerializeField] private int currentAnimalIndex = 0;
    [SerializeField] private int currentQuiz = 0;
    [FormerlySerializedAs("currentAnimalData")] [SerializeField] private WordData currentWordData;
    [SerializeField] private Button backButton;
    private string topic = "";
    private string subCategrgy = "";
    private void Start()
    {
        backButton.onClick.AddListener(onClickBackButton);
        setUpData();
    }
    
    public void ShowFlashcard()
    {   
        flashcardPanel.SetActive(true);
        currentWordData = listAnimals[0];
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateCard(currentWordData);
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
        currentWordData = listAnimals[currentAnimalIndex];
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateCard(currentWordData);
        quizPanel.SetActive(false);
    }


    public void ShowQuiz()
    {
        flashcardPanel.SetActive(false);
        if (currentAnimalIndex < listAnimals.Count - 1 )
        {
            currentAnimalIndex++;
        }      
        quizPanel.GetComponent<QuizManager>().UpdateUI(currentWordData);
        quizPanel.SetActive(true);
    }
    
    private void setUpData()
    {
         subCategrgy = PlayerPrefs.GetString("SelectedSubCategory");
         string mainCategory =  PlayerPrefs.GetString("SelectedMainCategoryId");
         FirebaseDatabaseManager.Instance.LoadWords(mainCategory,subCategrgy, OnWordsLoaded);
    }

    void OnWordsLoaded(List<WordData> words)
    {
        if (words == null)
        {
            Debug.LogError("Không tải được dữ liệu!");
            return;
        }

        Debug.Log("✅ Đã load " + words.Count + " từ!");
        foreach (var w in words)
        {
            Debug.Log($"{w.nameEn} - {w.nameVi}");
        }
        listAnimals.AddRange(words);
        quizPanel.GetComponent<QuizManager>().initQuiz(listAnimals);
        ShowFlashcard();
    }
    
    void onClickBackButton()
    {
        SceneManager.LoadSceneAsync("HomeScene");
    }
    
    async void ShowFinishPanel()
    {
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        //ApiController.Instance.SaveUserCategoryHistory(userId, catogeryId, ApiController.CategoryType.Vocabulary);
        await FirebaseDatabaseManager.Instance.CompleteMissionById(GlobalData.MissionKeys.LEARN_NEW);
        GameEvents.ShowNotifcation("Bạn đã hoàn thành khoá học.\n Sẽ Trở Về Trang Chủ",Color.black);
    }
}
