using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceQuizJSON : MonoBehaviour
{
    [Header("UI")]
    public Image questionImage;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI questionNumberText;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    
    [Header("Feedback")]
    public GameObject correctPanel;
    public GameObject wrongPanel;
    
    private List<AnimalData> allAnimals;
    private List<AnimalData> quizAnimals;
    private int currentQuestion = 0;
    private int correctAnswers = 0;
    private int totalQuestions = 10;
    
    void Start()
    {
        // Get all animals for wrong answers
        allAnimals = new List<AnimalData>(VocabularyDataManager.Instance.allAnimals);
        
        // Get selected category
        string category = PlayerPrefs.GetString("SelectedCategory", "All");
        
        if (category == "All")
        {
            quizAnimals = VocabularyDataManager.Instance.GetRandomAnimals(totalQuestions);
        }
        else
        {
            quizAnimals = VocabularyDataManager.Instance.GetRandomAnimalsFromCategory(category, totalQuestions);
        }
        
        ShowQuestion();
    }
    
    void ShowQuestion()
    {
        if (currentQuestion >= totalQuestions)
        {
            EndQuiz();
            return;
        }
        
        AnimalData correct = quizAnimals[currentQuestion];
        
        // Show image and question
        questionImage.sprite = correct.sprite;
        questionText.text = "What is this animal?";
        
        // Generate wrong answers
        List<AnimalData> wrongAnimals = new List<AnimalData>(allAnimals);
        wrongAnimals.Remove(correct);
        
        List<AnimalData> options = new List<AnimalData> { correct };
        for (int i = 0; i < 3; i++)
        {
            int rand = UnityEngine.Random.Range(0, wrongAnimals.Count);
            options.Add(wrongAnimals[rand]);
            wrongAnimals.RemoveAt(rand);
        }
        
        // Shuffle options
        for (int i = 0; i < options.Count; i++)
        {
            var temp = options[i];
            int rand = UnityEngine.Random.Range(i, options.Count);
            options[i] = options[rand];
            options[rand] = temp;
        }
        
        // Setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            AnimalData option = options[i];
            
            var btnText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = $"{option.name_en}\n({option.name_vi})";
            
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(option, correct));
            answerButtons[i].interactable = true;
        }
        
        UpdateUI();
    }
    
    void CheckAnswer(AnimalData selected, AnimalData correct)
    {
        // Disable all buttons
        foreach (var btn in answerButtons)
            btn.interactable = false;
        
        if (selected.id == correct.id)
        {
            correctAnswers++;
            ShowFeedback(true);
            
            // Play correct sound
            if (audioSource && correctSound)
                audioSource.PlayOneShot(correctSound);
            
            // Play animal sound
            if (correct.audioClip)
                audioSource.PlayOneShot(correct.audioClip);
        }
        else
        {
            ShowFeedback(false);
            
            if (audioSource && wrongSound)
                audioSource.PlayOneShot(wrongSound);
        }
        
        Invoke("NextQuestion", 2f);
    }
    
    void ShowFeedback(bool isCorrect)
    {
        if (isCorrect && correctPanel)
        {
            correctPanel.SetActive(true);
            Invoke("HideFeedback", 1.5f);
        }
        else if (!isCorrect && wrongPanel)
        {
            wrongPanel.SetActive(true);
            Invoke("HideFeedback", 1.5f);
        }
    }
    
    void HideFeedback()
    {
        if (correctPanel) correctPanel.SetActive(false);
        if (wrongPanel) wrongPanel.SetActive(false);
    }
    
    void NextQuestion()
    {
        currentQuestion++;
        ShowQuestion();
    }
    
    void UpdateUI()
    {
        scoreText.text = $"Score: {correctAnswers}/{totalQuestions}";
        questionNumberText.text = $"Question {currentQuestion + 1}/{totalQuestions}";
    }
    
    void EndQuiz()
    {
        float percentage = (float)correctAnswers / totalQuestions;
        int stars = percentage >= 0.9f ? 3 : percentage >= 0.7f ? 2 : 1;
        
        Debug.Log($"Quiz Complete! Score: {correctAnswers}/{totalQuestions}, Stars: {stars}");
        
        // Save results and return to menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}