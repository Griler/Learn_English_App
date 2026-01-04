using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LessonManager : MonoBehaviour
{
    [Header("UI Panels")] public GameObject flashcardPanel;
    public GameObject container;
    public GameObject quizPanel;
    public Button nextButton;
    private List<VocabItem>  listAnimals = new List<VocabItem> ();
    [SerializeField] private int currentAnimalIndex = 0;
    [SerializeField] private int currentQuiz = 0;

    [FormerlySerializedAs("currentAnimalData")] [SerializeField]
    private VocabItem currentLessonItem;

    [SerializeField] private Button backButton;
    [SerializeField] private GameObject panelNext;
    [SerializeField] private GameObject panelEnd;
    private string topic = "";
    private string subCategrgy = "";
    public VocabularyDatabase vocabDatabase;

    private void Awake()
    {
        container.SetActive(false);
    }

    private void Start()
    {
        backButton.onClick.AddListener(onClickBackButton);
        nextButton.onClick.AddListener(onClickNextButton);
        currentAnimalIndex = 0;
        LoadData();
    }

    public void ShowFlashcard()
    {
        if (listAnimals == null || listAnimals.Count == 0) return;

        flashcardPanel.SetActive(true);
        quizPanel.SetActive(false);

        // Lấy data theo index hiện tại chứ không lấy [0] nữa
        currentLessonItem = listAnimals[currentAnimalIndex];
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateCard(currentLessonItem);
        flashcardPanel.GetComponent<FlashCardSceneManager>().updateExample(currentLessonItem);
    }

    public void ShowFlashcardByButton()
    {
        currentAnimalIndex++;
        if (currentAnimalIndex >= listAnimals.Count)
        {
            quizPanel.GetComponent<QuizManager>().selectedTags.Add(subCategrgy);
            quizPanel.GetComponent<QuizManager>().StartQuiz();
            flashcardPanel.SetActive(false);
            quizPanel.SetActive(true);
            return;
        }

        ShowFlashcard();
    }

    public void ShowQuiz()
    {
        flashcardPanel.SetActive(false);
        quizPanel.SetActive(true);
        //quizPanel.GetComponent<QuizManager>().UpdateUI(currentLessonItem);
    }

    private void LoadData()
    {
        subCategrgy = PlayerPrefs.GetString("SelectedSubCategory");
        listAnimals = vocabDatabase.GetVocabsByTag(subCategrgy);
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
        GameEvents.ShowNotifcation("Bạn đã hoàn thành chủ đề học.\n Trở Về Trang Chủ chọn chủ đề khác",
            Color.black);
        panelEnd.SetActive(true);
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
