using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class FlashcardUIExampleController : FlashcardUIController
{
    [Header("UI Learn Example")] 
    [SerializeField] private TextMeshProUGUI ruleText;
    [SerializeField] private TextMeshProUGUI exampleText;
    [SerializeField] private TextMeshProUGUI translationText;
    [SerializeField] public List<GrammarFlashcardExmpale> listCardExample;

    private GrammarFlashcardExmpale currentGrammarFlashcardExmpale;
    private int cardExampleIndexCurrent = 0;

    protected override void Start()
    {
        base.Start();
        nextButton.onClick.AddListener(OnSubmitAnswer);
        listCardExample = GrammarManager.GetCardsToLearn();
        listCard.AddRange(listCardExample);
        if (listCardExample.Count > 0)
        {
            ShowCardLearn(listCardExample[cardExampleIndexCurrent]);
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
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnSubmitAnswer);
        SentenceSplitter(card.sentence, card.conjugatedVerb);
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
        }
        else
        {
            Debug.LogError($"Không tìm thấy động từ '{correctVerb}' trong câu!");
        }

        exampleQuestionText.text = $"{preVerb}  __________ {postVerb}";
        inputKeyBoardCustom.initButtonWord(conjugatedVerb);
    }

    public override void HandleAnswer(Enum type)
    {
        base.HandleAnswer(type);
        if (Convert.ToInt32(type) == 0)
        {
            cardExampleIndexCurrent++;
            if (cardExampleIndexCurrent < listCardExample.Count)
            {
                ShowCardLearn(listCardExample[cardExampleIndexCurrent]);
            }
            else
            {
                ShowFinishPanel();
            }
        }
    }

    void ShowFinishPanel()
    {
        setActiveFlashCard(false);
        GameEvents.ShowNotifcation("Bạn đã hoàn thành khoá học. Bạn có muốn làm bài luyện tập nhanh không ?",Color.black);
        UpdateMissionState();
    }
    
    private async void UpdateMissionState()
    {
        await FirebaseDatabaseManager.Instance.CompleteMissionById(GlobalData.MissionKeys.LEARN_NEW);
    }


    public override void OnSubmitAnswer()
    {
        ShowFinishPanel();
        return;
        string userAnswer = verbInputField.text.ToLower();
        string correctAnswer = currentGrammarFlashcardExmpale.conjugatedVerb.ToLower();
        if (userAnswer == correctAnswer)
        {
            HandleAnswer(TypeInputField.Correct);
           
        }
        else
        {
            HandleAnswer(TypeInputField.Wrong);
        }
        
    }
}