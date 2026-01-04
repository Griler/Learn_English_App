using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum QuizType
{
    TextQuiz, // Quiz về text: hiện text VI → chọn text EN (hoặc ngược lại)
    ExampleQuiz // Quiz về example: hiện example VI → chọn example EN (hoặc ngược lại)
}

public enum QuestionLanguage
{
    English,
    Vietnamese
}

[System.Serializable]
public class QuizManager : MonoBehaviour
{
    [Header("References")] public VocabularyDatabase vocabDatabase;

    [Header("UI Elements")] public TextMeshProUGUI questionText;
    public TextMeshProUGUI questionTypeText;
    public Button[] answerButtons; // 4 buttons cho 4 đáp án
    public TextMeshProUGUI resultText;
    public Button nextQuestionButton;
    public Button restartButton;

    [Header("Quiz Settings")] public int totalQuestions = 10;
    public int numberOfAnswerChoices = 4;

    [Header("Tag Selection")] public List<string> selectedTags = new List<string>();

    private List<VocabItem> quizVocabulary;
    private List<QuizQuestion> questions;
    private List<QuizQuestion> questionsCorrect;
    private List<QuizQuestion> questionsWrong;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private QuizQuestion currentQuestion;

    void Start()
    {
        nextQuestionButton.onClick.AddListener(LoadNextQuestion);
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartQuiz);
            restartButton.gameObject.SetActive(false);
        }
        
        // Setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i; // Capture cho closure
            Button button = answerButtons[index];
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(button, index));

            // Add AnswerButton component nếu chưa có
            if (answerButtons[i].GetComponent<AnswerButton>() == null)
            {
                answerButtons[i].gameObject.AddComponent<AnswerButton>();
            }
        }
    }

    public void StartQuiz(List<string> tags)
    {
        selectedTags = tags;
        InitializeQuiz();
    }

    public void StartQuiz()
    {
        if (selectedTags.Count == 0)
        {
            Debug.LogWarning("No tags selected!");
            return;
        }

        InitializeQuiz();
    }

    void InitializeQuiz()
    {
        // Lấy vocabulary theo tags đã chọn
        quizVocabulary = vocabDatabase.GetVocabsByTags(selectedTags);

        if (quizVocabulary.Count < numberOfAnswerChoices)
        {
            Debug.LogError($"Not enough vocabulary! Need at least {numberOfAnswerChoices} items.");
            return;
        }

        // Tạo danh sách câu hỏi
        GenerateQuestions();

        // Reset
        currentQuestionIndex = 0;
        correctAnswers = 0;

        // Hiển thị câu hỏi đầu tiên
        ShowCurrentQuestion();

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }
    }

    void GenerateQuestions()
    {
        questions = new List<QuizQuestion>();

        // Shuffle vocabulary
        List<VocabItem> shuffled = quizVocabulary.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < Mathf.Min(totalQuestions, shuffled.Count); i++)
        {
            QuizQuestion question = new QuizQuestion();

            // Chọn từ đúng
            question.correctAnswer = shuffled[i];

            // Random quiz type: TextQuiz hoặc ExampleQuiz
            question.quizType = Random.value > 0.5f ? QuizType.TextQuiz : QuizType.ExampleQuiz;

            // Random ngôn ngữ câu hỏi (EN hoặc VI)
            // Câu trả lời sẽ tự động là ngôn ngữ ngược lại
            question.questionLanguage = Random.value > 0.5f
                ? QuestionLanguage.English
                : QuestionLanguage.Vietnamese;

            // Chọn các đáp án sai
            question.wrongAnswers = new List<VocabItem>();
            List<VocabItem> availableWrong = shuffled.Where(v => v.id != question.correctAnswer.id).ToList();

            for (int j = 0; j < numberOfAnswerChoices - 1; j++)
            {
                if (availableWrong.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableWrong.Count);
                    question.wrongAnswers.Add(availableWrong[randomIndex]);
                    availableWrong.RemoveAt(randomIndex);
                }
            }

            questions.Add(question);
        }
    }

    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            ShowFinalResult();
            return;
        }

        currentQuestion = questions[currentQuestionIndex];

        // Hiển thị câu hỏi
        questionText.text = currentQuestion.GetQuestionText();

        // Hiển thị loại câu hỏi
        //questionTypeText.text = GetQuizTypeDescription(currentQuestion);

        // Tạo danh sách tất cả đáp án và shuffle
        List<VocabItem> allAnswers = new List<VocabItem>();
        allAnswers.Add(currentQuestion.correctAnswer);
        allAnswers.AddRange(currentQuestion.wrongAnswers);
        allAnswers = allAnswers.OrderBy(x => Random.value).ToList();

        // Hiển thị đáp án lên buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < allAnswers.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = currentQuestion.GetAnswerText(allAnswers[i]);
                answerButtons[i].interactable = true;

                // Lưu vocab item vào button để check sau
                answerButtons[i].GetComponent<AnswerButton>().vocabItem = allAnswers[i];
                answerButtons[i].GetComponent<Image>().color =  new Color32(86, 107, 132, 255);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        resultText.text = "";
        nextQuestionButton.interactable = false;
    }

    string GetQuizTypeDescription(QuizQuestion question)
    {
        string questionLang = question.questionLanguage == QuestionLanguage.English ? "EN" : "VI";
        string answerLang = question.AnswerLanguage == QuestionLanguage.English ? "EN" : "VI";

        if (question.quizType == QuizType.TextQuiz)
        {
            return $"TextMeshProUGUI ({questionLang}) → TextMeshProUGUI ({answerLang})";
        }
        else // ExampleQuiz
        {
            return $"Example ({questionLang}) → Example ({answerLang})";
        }
    }

    void OnAnswerSelected(Button button,int buttonIndex)
    {
        AnswerButton answerBtn = answerButtons[buttonIndex].GetComponent<AnswerButton>();

        if (answerBtn.vocabItem.id == currentQuestion.correctAnswer.id)
        {
            // Đúng
            correctAnswers++;
            ShowResult(true,button );
        }
        else
        {
            // Sai
            ShowResult(false, button);
        }

        // Disable tất cả buttons
        foreach (var btn in answerButtons)
        {
            btn.interactable = false;
        }
    }

    void ShowResult(bool isCorrect,Button selectButton)
    {

        if (isCorrect)
        {
            resultText.text = "Correct!";
            resultText.color = Color.lightGreen;
            selectButton.GetComponent<Image>().color = Color.lightGreen;
        }
        else
        {
            string correctAnswerText = currentQuestion.GetAnswerText(currentQuestion.correctAnswer);
            resultText.text = $"Wrong!";
            resultText.color = Color.orangeRed;
            selectButton.GetComponent<Image>().color = Color.orangeRed;;
        }

        for (int buttonIndex = 0; buttonIndex < answerButtons.Length; buttonIndex++)
        {
            AnswerButton answerBtn = answerButtons[buttonIndex].GetComponent<AnswerButton>();
            if(answerBtn.vocabItem.id == currentQuestion.correctAnswer.id)
            {
                answerBtn.GetComponent<Image>().color = Color.lawnGreen;
            }
        }
        nextQuestionButton.interactable = true;
    }

    void LoadNextQuestion()
    {
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    void ShowFinalResult()
    {
        questionText.text = "Quiz Completed!";

        float percentage = (float)correctAnswers / questions.Count * 100f;


        string grade = "";
        if (percentage >= 90) grade = "Excellent! 🌟";
        else if (percentage >= 70) grade = "Good Job! 👍";
        else if (percentage >= 50) grade = "Keep Practicing! 💪";
        else grade = "Need More Practice 📚";

        resultText.text = $"Final Score: {correctAnswers}/{questions.Count}\n{percentage:F1}%\n\n{grade}";
        resultText.color = percentage >= 70 ? Color.green : (percentage >= 50 ? Color.yellow : Color.red);

        nextQuestionButton.gameObject.SetActive(false);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        foreach (var btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }
    }

    public void RestartQuiz()
    {
        nextQuestionButton.gameObject.SetActive(true);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }

        InitializeQuiz();
    }
}