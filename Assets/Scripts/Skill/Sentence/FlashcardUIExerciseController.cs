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
    public Button nextSkipButton;
    
    private void OnEnable()
    {
        flashCardContainer.SetActive((false));
        GameEvents.showExerciseUI += showExerciseUI;
        skipButton.onClick.AddListener((() =>
        {
            resultText.text = currentGrammarFlashcardExercise.answer;
            resultText.color = Color.forestGreen;
            nextSkipButton.gameObject.SetActive(true);
        }));
        nextSkipButton.onClick.AddListener(() =>
        {
            cardExerciseIndexCurrent++;
            if (cardExerciseIndexCurrent < listCardExercise.Count)
            {
                updateProgressBar();
                ShowCardExercise(listCardExercise[cardExerciseIndexCurrent]);
            }
            else
            {
                updateProgressBar();
                ShowFinishPanel();
            }
        });
    }

    private void OnDestroy()
    {
        GameEvents.showExerciseUI -= showExerciseUI;
        skipButton.onClick.RemoveAllListeners();
    }
     
        
    public void showExerciseUI()
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
        grammarId.text = grammarDefinitions[card.grammarPointID.ToUpper()].ToUpper();
        ruleText.text = "Quy tắc: " + card.ruleText;
        nextSkipButton.gameObject.SetActive(false);
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
    
    protected override void updateProgressBar()
    {
        float incrementValue = (progressBar.maxValue / listCardExercise.Count);
        progressBar.value = progressBar.value + incrementValue;
    }
}