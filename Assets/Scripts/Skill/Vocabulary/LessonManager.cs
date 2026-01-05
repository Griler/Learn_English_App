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
    [SerializeField] protected Slider progressBar;

    private int countItem = 0;
    private void Awake()
    {
        container.SetActive(false);
    }

    private void Start()
    {
        backButton.onClick.AddListener(onClickBackButton);
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
        flashcardPanel.GetComponent<FlashCardSceneManager>().setUpCard(currentLessonItem);
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
    
    public void updateProgressBar()
    {
        float incrementValue = (progressBar.maxValue / listAnimals.Count);
        progressBar.value = progressBar.value + incrementValue;
    }
}
