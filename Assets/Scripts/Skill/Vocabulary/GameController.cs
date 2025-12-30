using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI Panels")] public GameObject flashcardPanel;
    public GameObject container;
    public GameObject quizPanel;
    public Button nextButton;
    private List<WordData> listAnimals = new List<WordData>();
    [SerializeField] private int currentAnimalIndex = 0;
    [SerializeField] private int currentQuiz = 0;

    [FormerlySerializedAs("currentAnimalData")] [SerializeField]
    private WordData currentWordData;

    [SerializeField] private Button backButton;
    [SerializeField] private GameObject panelNext;
    [SerializeField] private GameObject panelEnd;
    private string topic = "";
    private string subCategrgy = "";

    private void Awake()
    {
        container.SetActive(false);
    }

    private void Start()
    {
        backButton.onClick.AddListener(onClickBackButton);
        nextButton.onClick.AddListener(onClickNextButton);
        currentAnimalIndex = 0;
        setUpData();
    }

    public void ShowFlashcard()
    {
        if (listAnimals == null || listAnimals.Count == 0) return;

        flashcardPanel.SetActive(true);
        quizPanel.SetActive(false);

        // Lấy data theo index hiện tại chứ không lấy [0] nữa
        currentWordData = listAnimals[currentAnimalIndex];
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateCard(currentWordData);
    }

    public void ShowFlashcardByButton()
    {
        bool isCorrect = quizPanel.GetComponent<QuizManager>().getCorrectAnswer();
        if (!isCorrect) return;
        currentAnimalIndex++;
        if (currentAnimalIndex >= listAnimals.Count)
        {
            ShowFinishPanel();
            return;
        }

        StartCoroutine(showNextFlashCard());
    }

    IEnumerator showNextFlashCard()
    {
        yield return new WaitForSeconds(0.75f);
        ShowFlashcard();
    }


    public void ShowQuiz()
    {
        flashcardPanel.SetActive(false);
        quizPanel.SetActive(true);
        quizPanel.GetComponent<QuizManager>().UpdateUI(currentWordData);
    }

    private void setUpData()
    {
        subCategrgy = PlayerPrefs.GetString("SelectedSubCategory");
        string mainCategory = PlayerPrefs.GetString("SelectedMainCategoryId");
        Debug.Log("index" + GameSessionData.mapSubTopics[subCategrgy]);
        Debug.Log("test" + GameSessionData.mapSubTopics.ContainsValue(10));
        Debug.Log("test" + GameSessionData.mapSubTopics.ContainsValue(1));
        FirebaseDatabaseManager.Instance.LoadWords(mainCategory, subCategrgy, OnWordsLoaded);
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
        container.SetActive(true);
        ShowFlashcard();
    }

    public void onClickBackButton()
    {
        SceneManager.LoadSceneAsync("HomeScene");
    }

    async void ShowFinishPanel()
    {
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        //ApiController.Instance.SaveUserCategoryHistory(userId, catogeryId, ApiController.CategoryType.Vocabulary);
        string mainTopic = PlayerPrefs.GetString("SelectedMainCategoryId");
        FirebaseDatabaseManager.Instance.SaveUserProgress(mainTopic, subCategrgy, GameSessionData.CurrentSubTopics);
        await FirebaseDatabaseManager.Instance.CompleteMissionById(GlobalData.MissionKeys.LEARN_VOCA);
        int currentIndex = GameSessionData.mapSubTopics[subCategrgy];
        int nextCurrentIndex = currentIndex + 1;
        if (GameSessionData.mapSubTopics.ContainsValue(nextCurrentIndex))
        {
            GameEvents.ShowNotifcation("Bạn đã hoàn thành bài học.\n Bạn có muốn làm bài tập khác không ?",
                Color.black);
            panelNext.SetActive(true);
        }
        else
        {
            GameEvents.ShowNotifcation("Bạn đã hoàn thành chủ đề học.\n Trở Về Trang Chủ chọn chủ đề khác",
                Color.black);
            panelEnd.SetActive(true);
        }
    }

    void onClickNextButton()
    {
        int currentIndex = GameSessionData.mapSubTopics[subCategrgy];
        int nextCurrentIndex = currentIndex + 1;
        if (GameSessionData.mapSubTopics.ContainsValue(nextCurrentIndex))
        {
            string nextSubtopic = GlobalData.GetKeyByValue(GameSessionData.mapSubTopics, nextCurrentIndex);
            PlayerPrefs.SetString("SelectedSubCategory", nextSubtopic);
            SceneManager.LoadScene(GlobalData.flashCardScene);
        }   
        else
        {
            SceneManager.LoadScene("HomeScene");
        }
    }
}
