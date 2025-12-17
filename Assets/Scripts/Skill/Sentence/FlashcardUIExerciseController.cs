﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class FlashcardUIExerciseController : FlashcardUIController
{
    [SerializeField] protected List<GrammarFlashcardExercise> listCardExercise;
    private int cardExerciseIndexCurrent = 0;
    private GrammarFlashcardExercise currentGrammarFlashcardExercise;
    
    private void OnEnable()
    {
        GameEvents.showExerciseUI += showExerciseUI;
    }

    private void OnDestroy()
    {
        GameEvents.showExerciseUI -= showExerciseUI;
    }
     
        
    private void showExerciseUI()
    {
       setActiveFlashCard(true);
       initUI();
    }

    void initUI()
    {
        listCardExercise = GrammarManager.GetCardsToWrite();
        listCard.AddRange(listCardExercise);
        if (listCard.Count > 0)
        {
            ShowCardExercise(listCardExercise[cardExerciseIndexCurrent]);
        }
    }
    
    void ShowCardExercise(GrammarFlashcardExercise card)
    {
        StartCoroutine(setTypeInputField(TypeInputField.Default));
        currentGrammarFlashcardExercise = card;
        exampleQuestionText.text = card.question;
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
        await FirebaseDatabaseManager.Instance.CompleteMissionById(GlobalData.MissionKeys.PRACTICE3);
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