using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizAnswerItem : BaseCode
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI answerText;
    [SerializeField] private Button voiceButton;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI indexText;

    [Header("Quiz Data")]
    [SerializeField] private QuizAnswer question;
    [SerializeField] private int index;
    
    [Header("Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;
    private void Start()
    {
        // Gắn sự kiện cho button
        if (voiceButton != null)
        {
            voiceButton.onClick.AddListener(playVoice);
        }

        // Khởi tạo text
        UpdateUI();
    }

    // Set dữ liệu cho item
    public void SetQuizData(int idx ,QuizAnswer q)
    {
        index = idx;
        question = q;
        ResetColor();
        UpdateUI();
    }

    // Cập nhật UI
    private void UpdateUI()
    {
        if (questionText != null)
        {
            questionText.text = question.quizQuestion.quizType == QuizType.TextQuiz ?
                question.quizQuestion.correctAnswer.text.en : question.quizQuestion.correctAnswer.example.en; 
        }
        
        if (answerText != null)
        {
            answerText.text = question.quizQuestion.quizType == QuizType.TextQuiz ?
                question.quizQuestion.correctAnswer.text.vi : question.quizQuestion.correctAnswer.example.vi;
        }
        
        indexText.text = index.ToString();
        questionText.color = question.isCorrect ? correctColor : incorrectColor;
        answerText.color = question.isCorrect ? correctColor : incorrectColor;
        borderImage.color = question.isCorrect ? correctColor : incorrectColor;
        
    }

    // Kiểm tra đáp án
    public void playVoice()
    {
        audioManager.playVoiceWord(questionText.text);
    }

    // Reset màu về mặc định
    public void ResetColor()
    {
        if (answerText != null)
            answerText.color = defaultColor;
    }
}
