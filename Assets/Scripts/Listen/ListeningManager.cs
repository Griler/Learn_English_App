using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using Random = UnityEngine.Random; // Dùng để trộn đáp án

public class ListeningGameManager : MonoBehaviour
{
    [Header("--- DATA CONFIG ---")]
    // Bạn nhập dữ liệu thủ công vào List này trên Inspector
    public List<ListeningQuestion> questions;

    [Header("--- AUDIO ---")] public AudioSource audioSource;

    [Header("--- UI COMMON ---")]
    public TextMeshProUGUI questionInstruction; // VD: "Nghe và chọn đáp án"
    public Button playAudioBtn;
    public TextMeshProUGUI feedbackText;
    public Button nextBtn;
    public GameObject loadingPanel; // Panel Loading xoay xoay

    [Header("--- UI MULTIPLE CHOICE ---")] public GameObject choiceContainer;
    public Button[] choiceButtons; 
    public Button nextBtnMultipleChoice;

    [Header("--- UI TYPING ---")] public GameObject typingContainer;
    public TMP_InputField inputField;
    public Button submitTypingBtn;
    public Button nextBtnTyping;

    [Header("Feedback")] 
    public Color clickColor;
    public Color wrongColor;
    public Color correctColor;
    public Color defaultColor;
    
    private int currentIndex = 0;
    private ListeningQuestion currentQ;
    private GameObject currentButtonClick;
    [SerializeField] private TextMeshProUGUI resultText;
    private string chooseAnswer = "";

    private void Start()
    {
        playAudioBtn.onClick.AddListener(PlayCurrentAudio);
        nextBtn.onClick.AddListener(HandleAnswer);
        //submitTypingBtn.onClick.AddListener(CheckTypingAnswer);
        if (GlobalData.questionsToListen != null && GlobalData.questionsToListen.Count > 0)
        {
            questions.AddRange(GlobalData.questionsToListen);
        }

        // Kiểm tra Service
        if (GoogleSpeechService.Instance == null)
        {
            return;
        }

        LoadQuestion(0);
    }

    // --- LOGIC CHUYỂN CÂU HỎI ---
    void NextQuestion()
    {
        currentIndex++;
        if (currentIndex < questions.Count)
        {
            LoadQuestion(currentIndex);
        }
        else
        {
            // Hết câu hỏi
            nextBtn.gameObject.SetActive(false);
            choiceContainer.SetActive(false);
            typingContainer.SetActive(false);
            questionInstruction.text = "";
        }
    }

    void LoadQuestion(int index)
    {
        // Reset UI
        resultText.text = "";
        nextBtn.interactable = false;
        nextBtn.GetComponent<Image>().color = Color.darkGray;
        inputField.text = "";
        choiceContainer.SetActive(false);
        typingContainer.SetActive(false);

        currentQ = questions[index];
        
        setUpGame();
        DOVirtual.DelayedCall(0.5f, () =>
        {
            GoogleSpeechService.Instance.TextToSpeech(currentQ.correctAnswer,
                (clip =>
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }),
                s => Debug.LogError(s));
        });
    }

    // --- XỬ LÝ AUDIO ---
    void setUpGame()
    {
        //bool canPlayMultipleChoice = (currentQ.wrongAnswers != null && currentQ.wrongAnswers.Count >= 3);
        bool randomChoice = (Random.value > 0.5f); // Tung đồng xu

        // Nếu random trúng MC và dữ liệu đủ điều kiện -> Chơi MC
        if (false)
        {
            SetupMultipleChoice();
        }
        else
        {
            // Còn lại thì chơi Typing
            SetupTyping();
        }
    }

    void OnAudioError(string error)
    {
        if (loadingPanel) loadingPanel.SetActive(false);
        Debug.LogError("TTS Error: " + error);
    }

    public void PlayCurrentAudio()
    {
        if (audioSource.clip != null) audioSource.Play();
    }

    // --- LOGIC TRẮC NGHIỆM (Dùng wrongAnswers bạn đã nhập) ---
    void SetupMultipleChoice()
    {
        questionInstruction.text = "Nghe và chọn đáp án đúng:";
        choiceContainer.SetActive(true);
        resetUi();
        // Gom đáp án đúng và các đáp án sai vào 1 list
        List<string> options = new List<string>();
        options.Add(currentQ.correctAnswer);

        if (currentQ.wrongAnswers != null)
        {
            options.AddRange(currentQ.wrongAnswers);
        }

        // Trộn vị trí (Shuffle)
        options = options.OrderBy(x => Random.value).ToList();

        // Gán vào nút
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i];

                // Lưu lại giá trị cho nút bấm
                string selectedText = options[i];
                choiceButtons[i].onClick.RemoveAllListeners();
                var btn =    choiceButtons[i];
                choiceButtons[i].onClick.AddListener(() => initChooseAnswer(btn.gameObject));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
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
        nextBtn.interactable = true;
        nextBtn.GetComponent<Image>().color = Color.white;
    }
    
    public void HandleAnswer()
    {
        bool isCorrect = chooseAnswer == currentQ.correctAnswer;
        StartCoroutine(ShowFeedback(isCorrect, currentButtonClick));
    }
    
    IEnumerator ShowFeedback(bool isCorrect, GameObject chooseButton)
    {
        if (isCorrect)
        {
            chooseButton.GetComponent<Image>().color = correctColor;
            resultText.GetComponent<TextMeshProUGUI>().text = "Correct";
            resultText.GetComponent<TextMeshProUGUI>().color = Color.lawnGreen;
            yield return new WaitForSeconds(0.5f);
            NextQuestion();
        }
        else if (!isCorrect)
        {
            chooseButton.GetComponent<Image>().color = wrongColor;
            resultText.GetComponent<TextMeshProUGUI>().text = "Wrong";
            resultText.GetComponent<TextMeshProUGUI>().color = Color.softRed;
            currentButtonClick = null;
            chooseAnswer = "";
            resultText.text = "";
            nextBtn.interactable = false;
            nextBtn.GetComponent<Image>().color = Color.darkGray;
            yield return new WaitForSeconds(0.5f);
            chooseButton.GetComponent<Image>().color = defaultColor;
        }
    }

    // --- LOGIC GÕ PHÍM ---
    void SetupTyping()
    {
        try
        {
            questionInstruction.text = "Nghe và viết lại câu:";
            typingContainer.SetActive(true);
            submitTypingBtn.interactable = true;
            inputField.ActivateInputField();
            typingContainer.GetComponent<InputKeyBoardCustom>().initButtonWord(currentQ.correctAnswer);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }

    }

    void CheckTypingAnswer()
    {
        // So sánh chuỗi (bỏ viết hoa thường, bỏ khoảng trắng thừa 2 đầu)
        string userInput = inputField.text.Trim().ToLower();
        string correct = currentQ.correctAnswer.Trim().ToLower();

        if (userInput == correct)
        {
            OnCorrect();
        }
        else
        {
            OnWrong();
        }
    }

    // --- KẾT QUẢ ---
    void OnCorrect()
    {
        nextBtn.gameObject.SetActive(true);

        // Khóa input
        choiceContainer.SetActive(false);
        submitTypingBtn.interactable = false;
    }

    void OnWrong()
    {
        feedbackText.text = "<color=red>Sai rồi, nghe lại nhé!</color>";
    }

    void resetUi()
    {
        foreach (Button answerButton in choiceButtons)
        {
            answerButton.GetComponent<Image>().color = defaultColor;
            answerButton.GetComponentInChildren<TextMeshProUGUI>().text = "";
        }

        //nextBtn.interactable = false;
    }
}