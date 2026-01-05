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

public class QuizAnswer
{
    public QuizQuestion quizQuestion;
    public bool isCorrect;
    public string userChoice;

    public QuizAnswer(QuizQuestion quizQuestion, bool isCorrect)
    {
        this.quizQuestion = quizQuestion;
        this.isCorrect = isCorrect;
    }
}

[System.Serializable]
public class QuizManager : BaseCode
{
    [Header("References")] public VocabularyDatabase vocabDatabase;

    [Header("UI Elements")] public TextMeshProUGUI questionText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultTextDashBoard;
    public Button[] answerButtons; // 4 buttons cho 4 đáp án
    public TextMeshProUGUI resultText;
    public Button nextQuestionButton;
    public Button restartButton;

    [Header("Quiz Settings")] public int totalQuestions = 10;
    public int numberOfAnswerChoices = 4;
    public GameObject quizAwsItem;
    public GameObject dashboardContainer;
    public GameObject quizContanier;
    public Transform layoutTransform;
    public Button voiceButtoon;
    [SerializeField] protected Slider progressBar;

    [Header("Tag Selection")] public List<string> selectedTags = new List<string>();

    private List<VocabItem> quizVocabulary;
    public List<QuizQuestion> questions;
    private List<QuizAnswer> quizAnswers = new List<QuizAnswer>();
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

        if (voiceButtoon)
        {
            voiceButtoon.onClick.AddListener(() =>
            {
                string textToVoice = currentQuestion.quizType == QuizType.TextQuiz ?
                    currentQuestion.correctAnswer.text.en : currentQuestion.correctAnswer.example.en;
                audioManager.playVoiceWord(textToVoice);
            });
        }
    }

    public void StartQuiz(List<string> tags)
    {
        selectedTags = tags;
        InitializeQuiz();
    }

    public void StartQuiz()
    {
        quizContanier.SetActive(true);
        dashboardContainer.SetActive(false);
        if (selectedTags.Count == 0)
        {
            Debug.LogWarning("No tags selected!");
            return;
        }

        InitializeQuiz();
    }

    public void InitializeQuiz()
    {
        progressBar.value = 0;
        
        // Lấy vocabulary theo tags đã chọn
        quizVocabulary = vocabDatabase.GetVocabsByTags(selectedTags);

        if (quizVocabulary.Count < numberOfAnswerChoices)
        {
            numberOfAnswerChoices = quizVocabulary.Count;
            Debug.LogError($"Not enough vocabulary! Need at least {numberOfAnswerChoices} items.");
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

    public void GenerateQuestions()
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
        
        string textToVoice = currentQuestion.quizType == QuizType.TextQuiz ?
            currentQuestion.correctAnswer.text.en : currentQuestion.correctAnswer.example.en;
        audioManager.playVoiceWord(textToVoice);

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
            QuizAnswer answer = new QuizAnswer(currentQuestion,true);
            quizAnswers.Add(answer);
        }
        else
        {
            ShowResult(false, button);
            QuizAnswer answer = new QuizAnswer(currentQuestion,false);
            quizAnswers.Add(answer);
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
            resultText.text = "Đúng!";
            resultText.color = Color.lightGreen;
            selectButton.GetComponent<Image>().color = Color.lightGreen;
        }
        else
        {
            string correctAnswerText = currentQuestion.GetAnswerText(currentQuestion.correctAnswer);
            resultText.text = "Sai!";
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
        updateProgressBar();
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    void ShowFinalResult()
    {

        float percentage = (float)correctAnswers / questions.Count * 100f;


        string grade = "";
        if (percentage >= 90) grade = "Xuất sắc!";
        else if (percentage >= 70) grade = "tốt lắm";
        else if (percentage >= 50) grade = "Cố gắng";
        else grade = "Cần luyện tập thêm";

        scoreText.text = $"Tổng điểm: {correctAnswers}/{questions.Count}\n{percentage:F1}%";
        resultTextDashBoard.text = grade;
        resultTextDashBoard.color = Color.green;
        scoreText.color = percentage >= 70 ? Color.green : (percentage >= 50 ? Color.yellow : Color.red);

        nextQuestionButton.gameObject.SetActive(false);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        foreach (var btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }

        InitializeDashBoard();
    }

    public void RestartQuiz()
    {
        nextQuestionButton.gameObject.SetActive(true);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }
        quizContanier.SetActive(true);
        dashboardContainer.SetActive(false);
        InitializeQuiz();
    }

    void InitializeDashBoard()
    {
        quizContanier.SetActive(false);
        dashboardContainer.SetActive(true);
        for (int i = 0; i < quizAnswers.Count; i++)
        {
            GameObject item = Instantiate(quizAwsItem,layoutTransform);
            item.SetActive(true);
            int index = i + 1;
            item.GetComponent<QuizAnswerItem>().SetQuizData(index, quizAnswers[i]);
        }
        saveUserProgress();
    }
    
    async void saveUserProgress()
    {
        string mainTopic = PlayerPrefs.GetString("SelectedMainCategoryId");
        string subTopc = PlayerPrefs.GetString("SelectedSubCategory");
        FirebaseDatabaseManager.Instance.SaveUserProgress(mainTopic, subTopc, GameSessionData.CurrentSubTopics);
        await FirebaseDatabaseManager.Instance.CompleteMissionById(GlobalData.MissionKeys.LEARN_VOCA);
    }
    
    public void updateProgressBar()
    {
        float incrementValue = (progressBar.maxValue / questions.Count);
        progressBar.value = progressBar.value + incrementValue;
    }
}