using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


[System.Serializable]
public class WordPair
{
    public string english;
    public string vietnamese;
    public string sprite;
}

public class QuizManager : BaseCode
{
    [Header("UI")] public Image questionImage;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI questionNumberText;

    [Header("Audio")] public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("Feedback")] 
    public Color clickColor;
    public Color wrongColor;
    public Color correctColor;
    public Color defaultColor;

    private List<AnimalData> allAnimals;
    public List<WordPair> vocabulary = new List<WordPair>();
    private int currentQuestion = 0;
    private GameObject currentButtonClick;
    private WordPair currentVocabulary;
    private string correctAnswers = "";
    private int totalQuestions = 10;
    private bool showEnglish = false;
    private string chooseAnswer = "";
    public void initQuiz(List<AnimalData> allAnimals)
    {
        foreach (var animalData in allAnimals)
        {
            string nameEn = animalData.name_en;
            string nameVi = animalData.name_vi;
            string nameSprite = config.formatSpriteName(animalData.name_en);
            vocabulary.Add(new WordPair { english = nameEn, vietnamese = nameVi, sprite = nameSprite });
        }

        ShowQuestion();
    }

    void ShowQuestion(WordPair previousWord = null)
    {
        showEnglish = Random.value > 0.5f;

        if (previousWord == null)
        {
            // Không có từ trước → chọn random
            currentVocabulary = vocabulary[Random.Range(0, vocabulary.Count)];
        }
        else
        {
            currentVocabulary = previousWord;
        }
        
        correctAnswers = !showEnglish ? currentVocabulary.english : currentVocabulary.vietnamese;
        // tạo danh sách 4 đáp án (1 đúng + 3 sai)
        List<WordPair> options = new List<WordPair> { currentVocabulary };
        while (options.Count < 4)
        {
            WordPair randomPair = vocabulary[Random.Range(0, vocabulary.Count)];
            if (!options.Contains(randomPair))
                options.Add(randomPair);
        }

        Shuffle(options);
        // hiển thị câu hỏi
        if (showEnglish)
        {
            questionText.text = $"Từ này có nghĩa là gì: {currentVocabulary.english}?";
        }
        else
        {
            questionText.text = $"Từ tiếng Anh của \"{currentVocabulary.vietnamese}\" là gì?";
        }

        // gán text và event cho button
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            var pair = options[i];

            string answerText = showEnglish ? pair.vietnamese : pair.english;
            try
            {
                btn.GetComponentInChildren<TextMeshProUGUI>().text = answerText;
                Debug.Log("btn: "+ answerText);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => initChooseAnswer(btn.gameObject));
        }
        //  UpdateUI();
    }

    void initChooseAnswer(GameObject chooseButton)
    {
       chooseAnswer = chooseButton.GetComponentInChildren<TextMeshProUGUI>().text;
       currentButtonClick = chooseButton;
       chooseButton.GetComponent<Image>().color = clickColor;
    }
    
    public void HandleAnswer()
    {
        if (chooseAnswer == correctAnswers)
        {
            StartCoroutine(ShowFeedback(true, currentButtonClick));
        }
    }
    IEnumerator ShowFeedback(bool isCorrect, GameObject chooseButton)
    {
        if (isCorrect)
        {
            chooseButton.GetComponent<Image>().color = correctColor;
            //resultText.color = Color.lawnGreen;
        }
        else if (!isCorrect)
        {
            chooseButton.GetComponent<Image>().color = wrongColor;
            //resultText.color = Color.softRed;
        }

        yield return new WaitForSeconds(0.75f);
        chooseButton.GetComponent<Image>().color = defaultColor;
    }

    void HideFeedback()
    {
    }

    void NextQuestion()
    {
        currentQuestion++;
        ShowQuestion();
    }

    void UpdateUI()
    {
        questionNumberText.text = $"Question {currentQuestion + 1}/{totalQuestions}";
    }

    void EndQuiz()
    {

    }

    public void Shuffle<T>(List<T> array)
    {
        for (int i = 0; i < array.Count; i++)
        {
            int randomIndex = Random.Range(i, array.Count); // UnityEngine.Random
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
        }
    }
}