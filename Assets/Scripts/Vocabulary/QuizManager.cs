using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


[System.Serializable]

public class QuizManager : BaseCode
{
    [Header("UI")] public Image questionImage;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI showText;

    [Header("Audio")] public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("Feedback")] 
    public Color clickColor;
    public Color wrongColor;
    public Color correctColor;
    public Color defaultColor;

    private List<AnimalData> allAnimals;
    public List<AnimalData> vocabulary = new List<AnimalData>();
    private int currentQuestion = 0;
    private GameObject currentButtonClick;
    private AnimalData currentVocabulary;
    private string correctAnswers = "";
    private int totalQuestions = 10;
    private bool showEnglish = false;
    private string chooseAnswer = "";
    [SerializeField]private Button nextButton;

    public void initQuiz(List<AnimalData> allAnimals)
    {
        vocabulary.AddRange(allAnimals);
    }

    public void ShowQuestion(AnimalData previousWord = null)
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
        
        correctAnswers = !showEnglish ? currentVocabulary.name_en : currentVocabulary.name_vi;
        // tạo danh sách 4 đáp án (1 đúng + 3 sai)
        List<AnimalData> options = new List<AnimalData> { currentVocabulary };
        while (options.Count < 4)
        {
            AnimalData randomPair = vocabulary[Random.Range(0, vocabulary.Count)];
            if (!options.Contains(randomPair))
                options.Add(randomPair);
        }

        Shuffle(options);
        
        string nameSprite = config.formatSpriteName(currentVocabulary.name_en);
        if(nameSprite != "")
            questionImage.sprite = assetManager.getSpriteAnimal(nameSprite);
        
        if (showEnglish)
        {
            questionText.text = $"Từ này có nghĩa là gì: {currentVocabulary.name_en}?";
            showText.text = currentVocabulary.name_en;
        }
        else
        {
            questionText.text = $"Từ tiếng Anh của \"{currentVocabulary.name_vi}\" là gì?";
            showText.text = currentVocabulary.name_vi;
        }

        // gán text và event cho button
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            var pair = options[i];

            string answerText = showEnglish ? pair.name_vi : pair.name_en;
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
    }

    void initChooseAnswer(GameObject chooseButton)
    {
        if (currentButtonClick != null)
        {
            currentButtonClick.GetComponent<Image>().color = defaultColor;
        }
       chooseAnswer = chooseButton.GetComponentInChildren<TextMeshProUGUI>().text;
       currentButtonClick = chooseButton;
       chooseButton.GetComponent<Image>().color = clickColor;
       nextButton.interactable = true;
    }

    public bool getCorrectAnswer()
    {
        return chooseAnswer == correctAnswers;
    }
    
    public void HandleAnswer()
    {
        bool isCorrect = chooseAnswer == correctAnswers;
        StartCoroutine(ShowFeedback(isCorrect, currentButtonClick));
    }
    IEnumerator ShowFeedback(bool isCorrect, GameObject chooseButton)
    {
        if (isCorrect)
        {
            chooseButton.GetComponent<Image>().color = correctColor;
            resultText.GetComponent<TextMeshProUGUI>().text = "Correct";
            resultText.GetComponent<TextMeshProUGUI>().color = Color.lawnGreen;
        }
        else if (!isCorrect)
        {
            chooseButton.GetComponent<Image>().color = wrongColor;
            resultText.GetComponent<TextMeshProUGUI>().text = "Wrong";
            resultText.GetComponent<TextMeshProUGUI>().color = Color.softRed;
            yield return new WaitForSeconds(0.75f);
            chooseButton.GetComponent<Image>().color = defaultColor;
            resultText.text = "";
            nextButton.interactable = false;
        }
    }


    public void UpdateUI(AnimalData previousWord = null)
    {
        resetUi();
        ShowQuestion(previousWord);
    }

    public void Shuffle<T>(List<T> array)
    {
        for (int i = 0; i < array.Count; i++)
        {
            int randomIndex = Random.Range(i, array.Count); // UnityEngine.Random
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
        }
    }

    void resetUi()
    {
        foreach (var answerButton in answerButtons)
        {
            answerButton.GetComponent<Image>().color = defaultColor;
            answerButton.GetComponentInChildren<TextMeshProUGUI>().text = "";
        }

        resultText.GetComponent<TextMeshProUGUI>().text = "";
        resultText.GetComponent<TextMeshProUGUI>().color = defaultColor;
        nextButton.interactable = false;
    }
}