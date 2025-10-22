using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private RectTransform layoutRect;

    [Header("UI Exercise Example")] public TextMeshProUGUI exerciseQuestionText;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject feedbackPanel; // Panel chứa các nút đánh giá
    [SerializeField] private TextMeshProUGUI resultText; // Text để báo Đúng/Sai

    //private GrammarFlashcard currentCard;
    [SerializeField] private List<GrammarFlashcardExmpale> listCardExample;
    [SerializeField] private List<GrammarFlashcardExercise> listCardExercise;
    private int currentCardIndex = 0;
    
    private GrammarFlashcardExmpale currentGrammarFlashcardExmpale;
    void Start()
    {
        GrammarManager = GetComponent<GrammarManager>();
        //feedbackPanel.SetActive(false);
        //resultText.gameObject.SetActive(false);

        // Lấy danh sách thẻ cần ôn tập
        listCardExample = GrammarManager.GetCardsToLearn();
        if (listCardExample.Count > 0)
        {
            ShowCardLearn(listCardExample[0]);
        }
        else
        {
            ruleText.text = "Bạn đã hoàn thành tất cả các thẻ ôn tập cho hôm nay!";
        }
    }

    void ShowCardLearn(GrammarFlashcardExmpale card)
    {
        currentGrammarFlashcardExmpale = card;
        ruleText.text = card.ruleText;
        exampleText.text = card.sentence;
        translationText.text = card.translation;
        SentenceSplitter(card.sentence, card.conjugatedVerb);
    }

    public void OnSubmitAnswer()
    {
        string userAnswer = verbInputField.text.ToLower();
        string correctAnswer = currentGrammarFlashcardExmpale.conjugatedVerb.ToLower();
        if (userAnswer == correctAnswer)
        {
            Debug.Log("Ngon");
            ruleText.text = "ngon";
        }
        else
        {
            Debug.Log("ga");
            ruleText.text = "fgdf";

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
        // 1. Dữ liệu đầu vào của bạn
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
            preVerb = fullSentence.Substring(0, verbIndex-1);
            int postIndex = verbIndex + correctVerb.Length;
            postVerb = fullSentence.Substring(postIndex);

            // In kết quả ra Console
            Debug.Log($"Câu gốc: '{fullSentence}'");
            Debug.Log($"Phần trước: '{preVerb}'"); // Kết quả: 'She '
            Debug.Log($"Phần sau: '{postVerb}'"); // Kết quả: ' the park.'
        }
        else
        {
            Debug.LogError($"Không tìm thấy động từ '{correctVerb}' trong câu!");
        }
        exampleQuestionText.text = $"{preVerb}  __________ {postVerb}";
    }
}