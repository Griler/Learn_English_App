﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


public class ListeningGameManager : MonoBehaviour
{
    public static ListeningGameManager Instance; // Singleton đơn giản để Handler gọi lại

    [Header("--- MODULES ---")]
    public ListeningMultipleChoiceHandler mcHandler;
    public ListeningTypingHandler typingHandler;
    public GameObject dashBoard;
    public GameObject container;
    public GameObject itemAnswer;
    public GameObject canavas;
    public TextMeshProUGUI textQuestion;

    [Header("--- DATA CONFIG ---")]
    public List<ListeningQuestion> questions = new List<ListeningQuestion>();
    public Dictionary<string, bool> answerChoose = new Dictionary<string, bool>();

    [Header("--- AUDIO ---")] 
    public AudioSource audioSource;

    [Header("--- UI COMMON ---")]
    public TextMeshProUGUI questionInstruction;
    public Button playAudioBtn;
    public TextMeshProUGUI resultText; 
    public Button nextBtn; 
    public Button skipBtn; 
    public GameObject loadingPanel;
    public Slider progressBar; 

    private int currentIndex = 0;
    private ListeningQuestion currentQ;
    
    private bool isModeMultipleChoice = false; 

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playAudioBtn.onClick.AddListener(PlayCurrentAudio);
        nextBtn.onClick.AddListener(HandleNextButton);
        skipBtn.onClick.AddListener(onSkipClick);
        canavas.SetActive(true);
        dashBoard.SetActive(false);
        if (GlobalData.questionsToListen != null && GlobalData.questionsToListen.Count > 0)
        {
            questions.Clear();
            questions.AddRange(GlobalData.questionsToListen);
            questions = questions.OrderBy(x => Random.value).Take(10).ToList();
        }

        if (GoogleSpeechService.Instance == null) return;

        LoadQuestion(0);
    }

    // --- LUỒNG CHÍNH ---
    void LoadQuestion(int index)
    {
        resultText.text = "";
        textQuestion.text = "Câu Hỏi: " + (currentIndex + 1);
        nextBtn.interactable = false;
        nextBtn.GetComponent<Image>().color = Color.darkGray;
        
        mcHandler.Hide();
        typingHandler.Hide();

        currentQ = questions[index];
        
        SetupGameMode();
        
        if (loadingPanel) loadingPanel.SetActive(true);
        DOVirtual.DelayedCall(0.5f, () =>
        {
            GoogleSpeechService.Instance.TextToSpeech(currentQ.correctAnswer,
                (clip =>
                {
                    if (loadingPanel) loadingPanel.SetActive(false);
                    audioSource.clip = clip;
                    audioSource.Play();
                }),
                s => {
                    if (loadingPanel) loadingPanel.SetActive(false);
                    Debug.LogError(s);
                });
        });
    }

    void SetupGameMode()
    {
        bool randomChoice = (Random.value > 0.5f); 

        if (randomChoice) 
        {
            isModeMultipleChoice = true;
            questionInstruction.text = "Nghe và chọn đáp án đúng:";
            mcHandler.gameObject.SetActive(true);
            mcHandler.Setup(currentQ);
        }
        else
        {
            typingHandler.gameObject.SetActive(true);
            isModeMultipleChoice = false;
            questionInstruction.text = "Nghe và viết lại từ:";
            typingHandler.Setup(currentQ, NextQuestion, onSkipClick);
        }
    }

    public void OnAnswerSelected()
    {
        nextBtn.interactable = true;
        nextBtn.GetComponent<Image>().color = Color.white;
    }

    public void HandleNextButton()
    {
        if (isModeMultipleChoice)
        {
            mcHandler.setInteractable(false);
            bool isCorrect = mcHandler.CheckAnswerAndShowFeedback();
            if (isCorrect)
            {
                StartCoroutine(ProcessCorrectAnswer());
            }
            else
            {
                StartCoroutine(ProcessWrongAnswerMC());
            }
        }
    }


    IEnumerator ProcessCorrectAnswer()
    {
        resultText.text = "Đúng";
        resultText.color = Color.green;
        ListenAnswer answer = new ListenAnswer();
        answerChoose[currentQ.correctAnswer] = true;
        yield return new WaitForSeconds(0.5f);
        NextQuestion();
    }

    IEnumerator ProcessWrongAnswerMC()
    {
        resultText.text = "Sai";
        resultText.color = Color.red;
        
        // Khóa nút next
        nextBtn.interactable = false;
        nextBtn.GetComponent<Image>().color = Color.darkGray;
        ListenAnswer answer = new ListenAnswer();
        answer.correctAws = currentQ.correctAnswer;
        answer.isCorrect = false;
        answerChoose[currentQ.correctAnswer] = false;
        yield return new WaitForSeconds(0.5f);
        
        resultText.text = "";
        mcHandler.ResetColorCurrentButton();
        NextQuestion();
    }

    void NextQuestion()
    {
        currentIndex++;
        if (currentIndex < questions.Count)
        {
            LoadQuestion(currentIndex);
            updateProgressBar();
        }
        else
        {
            // End Game
            canavas.SetActive(false);
            mcHandler.Hide();
            typingHandler.Hide();
            initDashBoad();
        }
    }

    void onSkipClick()
    {
        ListenAnswer answer = new ListenAnswer();
        answer.correctAws = currentQ.correctAnswer;
        answer.isCorrect = false;
        answerChoose[currentQ.correctAnswer] = false;
        NextQuestion();
    }

    public void PlayCurrentAudio()
    {
        if (audioSource.clip != null) audioSource.Play();
    }


    void initDashBoad()
    {
        dashBoard.SetActive(true);
        for (int i = 0; i < answerChoose.Count; i++)
        {
            GameObject item = Instantiate(itemAnswer, container.transform);
            item.SetActive(true);
            TextMeshProUGUI[] textComponents = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length > 0)
                textComponents[0].text = (i + 1).ToString();
            string currentKey = answerChoose.Keys.ElementAt(i);

            if (textComponents.Length > 1)
            {
                textComponents[1].text = currentKey;
                bool isCorrect = answerChoose[currentKey];

                if (isCorrect)
                {
                    textComponents[1].color = Color.green;
                }
                else
                {
                    textComponents[1].color = Color.red;
                }
            }
        }
    }

    public void onHomeButton()
    {
        SceneManager.LoadScene("HomeScene");
    }

    public void onPlayAgain()
    {
        SceneManager.LoadScene("ListenScene");
    }

    protected void updateProgressBar()
    {
        float incrementValue = (progressBar.maxValue / questions.Count);
        progressBar.value = progressBar.value + incrementValue;
    }
}