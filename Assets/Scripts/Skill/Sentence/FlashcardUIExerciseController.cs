using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FlashcardUIExerciseController : FlashcardUIController
{
    [SerializeField] protected List<GrammarFlashcardExercise> listCardExercise;
    private int cardExerciseIndexCurrent = 0;
    private GrammarFlashcardExercise currentGrammarFlashcardExercise;
    public Button skipButton;
    
    private void OnEnable()
    {
        flashCardContainer.SetActive((false));
        GameEvents.showExerciseUI += showExerciseUI;
        skipButton.onClick.AddListener((() =>
        {
            StartCoroutine(HandleSkipDelay());
        }));
    }
    
    private IEnumerator HandleSkipDelay()
    {
        // 1. Chờ 0.5 giây
        resultText.text = currentGrammarFlashcardExercise.answer;

        yield return new WaitForSeconds(0.5f);
        // 2. Chạy logic của bạn
        cardExerciseIndexCurrent++;
        if (cardExerciseIndexCurrent < listCardExercise.Count)
        {
            ShowCardExercise(listCardExercise[cardExerciseIndexCurrent]);
        }
        else
        {
            ShowFinishPanel();
        }
    }

    private void OnDestroy()
    {
        GameEvents.showExerciseUI -= showExerciseUI;
        skipButton.onClick.RemoveAllListeners();
    }
     
        
    private void showExerciseUI()
    {
       setActiveFlashCard(true);
       initUI();
    }

    void initUI()
    {
        listCardExercise = GrammarManager.GetCardsToWrite();
        progressBar.value = 0;
        cardExerciseIndexCurrent = 0;
        listCard.AddRange(listCardExercise);
        if (listCard.Count > 0)
        {
            ShowCardExercise(listCardExercise[cardExerciseIndexCurrent]);
        }
    }
    
    void ShowCardExercise(GrammarFlashcardExercise card)
    {
        StartCoroutine(setTypeInputField(TypeInputField.Default));
        verbInputField.text = "";
        currentGrammarFlashcardExercise = card;
        exampleQuestionText.text = card.question;
        grammarId.text = card.grammarPointID;
        ruleText.text = card.ruleText;
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnSubmitAnswer);
    }
    
    public override void HandleAnswer(Enum type)
    {
        base.HandleAnswer(type);
        if (Convert.ToInt32(type) == 0)
        {
            cardExerciseIndexCurrent++;
            if (cardExerciseIndexCurrent < listCardExercise.Count)
            {
                ShowCardExercise(listCardExercise[cardExerciseIndexCurrent]);
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
        GameEvents.ShowNotifcation("Bạn đã hoàn thành luyện tập. Bạn có muốn tiếp tục nữa không ?",Color.black);
        UpdateMissionState();
    }
    
    private async void UpdateMissionState()
    {
        await FirebaseDatabaseManager.Instance.CompleteMissionById(GlobalData.MissionKeys.LEARN_GRAMMAR);
    }
    
    public override void OnSubmitAnswer()
    {        
        Debug.Log("OnSubmitAnswer work");
        string userAnswer = verbInputField.text.ToLower();
        string correctAnswer = currentGrammarFlashcardExercise.answer.ToLower();
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