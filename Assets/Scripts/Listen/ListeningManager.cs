using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening; // Dùng để trộn đáp án

public class ListeningGameManager : MonoBehaviour
{
    [Header("--- DATA CONFIG ---")]
    // Bạn nhập dữ liệu thủ công vào List này trên Inspector
    public List<ListeningQuestion> questions;

    [Header("--- AUDIO ---")] public AudioSource audioSource;

    [Header("--- UI COMMON ---")] public TextMeshProUGUI questionInstruction; // VD: "Nghe và chọn đáp án"
    public Button playAudioBtn;
    public TextMeshProUGUI feedbackText;
    public Button nextBtn;
    public GameObject loadingPanel; // Panel Loading xoay xoay

    [Header("--- UI MULTIPLE CHOICE ---")] public GameObject choiceContainer;
    public Button[] choiceButtons; // Kéo 4 nút (A, B, C, D) vào đây

    [Header("--- UI TYPING ---")] public GameObject typingContainer;
    public TMP_InputField inputField;
    public Button submitTypingBtn;

    [Header("Feedback")] 
    public Color clickColor;
    public Color wrongColor;
    public Color correctColor;
    public Color defaultColor;
    
    private int currentIndex = 0;
    private ListeningQuestion currentQ;

    private void Start()
    {
        playAudioBtn.onClick.AddListener(PlayCurrentAudio);
        nextBtn.onClick.AddListener(NextQuestion);
        submitTypingBtn.onClick.AddListener(CheckTypingAnswer);
        if (GlobalData.questionsToListen.Count > 0)
        {
            questions.AddRange(GlobalData.questionsToListen);
        }

        // Kiểm tra Service
        if (GoogleSpeechService.Instance == null)
        {
            feedbackText.text = "Lỗi: Không tìm thấy GoogleSpeechService!";
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
            feedbackText.text = "CHÚC MỪNG! BẠN ĐÃ HOÀN THÀNH BÀI TẬP.";
            nextBtn.gameObject.SetActive(false);
            choiceContainer.SetActive(false);
            typingContainer.SetActive(false);
            questionInstruction.text = "";
        }
    }

    void LoadQuestion(int index)
    {
        // Reset UI
        feedbackText.text = "";
        nextBtn.interactable = false;
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
        if (randomChoice)
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
        feedbackText.text = "Lỗi tải âm thanh!";
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
                choiceButtons[i].onClick.AddListener(() => CheckMCAnswer(selectedText));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void CheckMCAnswer(string selected)
    {
        if (selected == currentQ.correctAnswer)
        {
            OnCorrect();
        }
        else
        {
            OnWrong();
        }
    }

    // --- LOGIC GÕ PHÍM ---
    void SetupTyping()
    {
        questionInstruction.text = "Nghe và viết lại câu:";
        typingContainer.SetActive(true);
        submitTypingBtn.interactable = true;
        inputField.ActivateInputField();
        typingContainer.GetComponent<InputKeyBoardCustom>().initButtonWord(currentQ.correctAnswer);
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
        feedbackText.text = "<color=green>CHÍNH XÁC!</color>";
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
        foreach (var answerButton in choiceButtons)
        {
            answerButton.GetComponent<Image>().color = defaultColor;
            answerButton.GetComponentInChildren<TextMeshProUGUI>().text = "";
        }
    }
}