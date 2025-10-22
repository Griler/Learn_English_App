using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
enum TypeInputField
{
    Wrong = 1,
    Correct = 0,
    Default = 2
}

public class FlashcardUIController : BaseCode
{
    public GrammarManager GrammarManager; // Kéo object chứa script GrammarManager vào đây

    [Header("UI Learn Example")] [SerializeField]
    private TextMeshProUGUI ruleText;

    [SerializeField] private TextMeshProUGUI exampleText;
    [SerializeField] private TextMeshProUGUI translationText;
    [SerializeField] private TextMeshProUGUI exampleQuestionText;
    [SerializeField] private TextMeshProUGUI preVerbTextUI;
    [SerializeField] private TMP_InputField verbInputField;

    [Header("UI Exercise Example")] public TextMeshProUGUI exerciseQuestionText;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject feedbackPanel; // Panel chứa các nút đánh giá

    [Header("UI FlashCard other")]
    [SerializeField] private InputKeyBoardCustom inputKeyBoardCustom;
    [SerializeField] private TextMeshProUGUI resultText; // Text để báo Đúng/Sai
    [SerializeField] private List<Sprite> backgroundInput;
    [SerializeField] private List<GrammarFlashcardExmpale> listCardExample;
    [SerializeField] private List<GrammarFlashcardExercise> listCardExercise;
    private int currentCardIndex = 0;

    private GrammarFlashcardExmpale currentGrammarFlashcardExmpale;
    private int cardExampleIndexCurrent = 0;
    void Start()
    {
        GrammarManager = GetComponent<GrammarManager>();
        //feedbackPanel.SetActive(false);
        //resultText.gameObject.SetActive(false);

        // Lấy danh sách thẻ cần ôn tập
        listCardExample = GrammarManager.GetCardsToLearn();
        if (listCardExample.Count > 0)
        {
            ShowCardLearn(listCardExample[cardExampleIndexCurrent]);
        }
        else
        {
            ruleText.text = "Bạn đã hoàn thành tất cả các thẻ ôn tập cho hôm nay!";
        }
    }

    void ShowCardLearn(GrammarFlashcardExmpale card)
    {
        StartCoroutine(setTypeInputField(TypeInputField.Default));
        currentGrammarFlashcardExmpale = card;
        ruleText.text = card.ruleText;
        exampleText.text = card.sentence;
        translationText.text = card.translation;
        resultText.text = "";
        resultText.color = Color.white;
        SentenceSplitter(card.sentence, card.conjugatedVerb);
    }

    public void OnSubmitAnswer()
    {
        string userAnswer = verbInputField.text.ToLower();
        string correctAnswer = currentGrammarFlashcardExmpale.conjugatedVerb.ToLower();
        if (userAnswer == correctAnswer)
        {
            StartCoroutine(HandleAnswer(TypeInputField.Correct));
           
        }
        else
        {
            StartCoroutine(setTypeInputField(TypeInputField.Wrong));
        }
        // resultText.gameObject.SetActive(true);
        // feedbackPanel.SetActive(true); // Hiển thị các nút đánh giá
        // submitButton.interactable = false;
    }

    public void onVoiceButtonVoice()
    {
        audioManager.SpeakToText(exampleText.text);
    }

    // Gán hàm này cho các nút đánh giá trong Unity Editor
    public void OnFeedbackButtonPressed(int quality)
    {
        // GrammarManager.UpdateCard(currentCard, quality);

        // Chuyển sang thẻ tiếp theo
        currentCardIndex++;
        if (currentCardIndex != null) //reviewQueue.Count)
        {
            //ShowCard(reviewQueue[currentCardIndex]);
        }
        else
        {
            // Hoàn thành
            ruleText.text = "Tuyệt vời! Bạn đã hoàn thành bài ôn tập hôm nay.";
            exampleText.text = "";
            exerciseQuestionText.text = "";
            answerInputField.gameObject.SetActive(false);
            submitButton.gameObject.SetActive(false);
            feedbackPanel.SetActive(false);
            resultText.gameObject.SetActive(false);
        }
    }

    public void SentenceSplitter(string sentence, string conjugatedVerb)
    {
        string fullSentence = sentence;
        string correctVerb = conjugatedVerb;

        string preVerb = "";
        string postVerb = "";


        int verbIndex = fullSentence.IndexOf(
            correctVerb,
            StringComparison.OrdinalIgnoreCase
        );

        if (verbIndex != -1) // Nếu tìm thấy động từ
        {
            preVerb = fullSentence.Substring(0, verbIndex - 1);
            int postIndex = verbIndex + correctVerb.Length;
            postVerb = fullSentence.Substring(postIndex);
            Debug.Log($"Câu gốc: '{fullSentence}'");
            Debug.Log($"Phần trước: '{preVerb}'");
            Debug.Log($"Phần sau: '{postVerb}'");
        }
        else
        {
            Debug.LogError($"Không tìm thấy động từ '{correctVerb}' trong câu!");
        }

        exampleQuestionText.text = $"{preVerb}  __________ {postVerb}";
        inputKeyBoardCustom.initButtonWord(conjugatedVerb);
    }


    IEnumerator setTypeInputField(Enum colorIndex)
    {
        verbInputField.GetComponent<Image>().sprite = backgroundInput[Convert.ToInt32(colorIndex)];
        if (Convert.ToInt32(colorIndex) == 0)
        {
            resultText.text = "Correct";
            resultText.color = Color.lawnGreen;
        }
        else if(Convert.ToInt32(colorIndex) == 1)
        {
            resultText.text = "Wrong";
            resultText.color = Color.softRed;
        }
        yield return new WaitForSeconds(0.75f);
        verbInputField.GetComponent<Image>().sprite = backgroundInput[Convert.ToInt32(TypeInputField.Default)];
        resultText.text = "";
        resultText.color = Color.white;
    }
    
    IEnumerator HandleAnswer(Enum type)
    {
        yield return setTypeInputField(type);
        cardExampleIndexCurrent++;
        ShowCardLearn(listCardExample[cardExampleIndexCurrent]);
    }
}