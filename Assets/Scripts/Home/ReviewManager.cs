using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ReviewQuestionType
{
    TextQuiz,      // Quiz text như cũ
    MatchingGame   // Game ghép cặp text-ảnh
}

[System.Serializable]
public class MatchingPairData
{
    public string textEN;
    public Sprite image;
    public VocabItem vocabItem; // Tham chiếu tới VocabItem nếu cần
}

[System.Serializable]
public class ReviewQuestion
{
    public ReviewQuestionType type;
    public QuizQuestion quizQuestion; // Dùng cho TextQuiz
    public List<MatchingPairData> matchingPairs; // Dùng cho MatchingGame (3 cặp)
}

public class ReviewManager : BaseCode
{
    [Header("Database")]
    public VocabularyDatabase vocabDatabase;

    [Header("Quiz Panel")]
    public GameObject quizPanel;
    public TextMeshProUGUI quizQuestionText;
    public Button[] quizAnswerButtons;
    public TextMeshProUGUI quizResultText;
    public Button quizVoiceButton;

    [Header("Matching Panel")]
    public GameObject matchingPanel;
    public Transform ButtonsContainer;
    public GameObject textButtonPrefab;
    public GameObject imageButtonPrefab;
    public TextMeshProUGUI matchingFeedbackText;

    [Header("Common UI")]
    public TextMeshProUGUI progressText;
    public Slider progressBar;
    public Button nextButton;
    public Button restartButton;

    [Header("Dashboard")]
    public GameObject dashboardPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalGradeText;
    public GameObject reviewAnswerItemPrefab;
    public Transform dashboardLayoutTransform;

    [Header("Settings")]
    public int totalQuestions = 10;
    public int numberOfAnswerChoices = 4;
    public List<string> selectedTags = new List<string>();
    public Dictionary<int,QuizAnswer> listQuizAnswer= new Dictionary<int, QuizAnswer>();
    // Private variables
    private List<VocabItem> reviewVocabulary;
    private List<ReviewQuestion> reviewQuestions;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private ReviewQuestion currentReviewQuestion;
    private bool questionAnswered = false;

    // Matching game variables
    private MatchingButton selectedTextButton;
    private MatchingButton selectedImageButton;
    private int matchedPairsCount = 0;
    private List<MatchingButton> allMatchingButtons = new List<MatchingButton>();

    void Start()
    {
        nextButton.onClick.AddListener(LoadNextQuestion);
        restartButton.onClick.AddListener(RestartReview);
        restartButton.gameObject.SetActive(false);

        // Setup quiz answer buttons
        for (int i = 0; i < quizAnswerButtons.Length; i++)
        {
            int index = i;
            Button button = quizAnswerButtons[index];
            quizAnswerButtons[i].onClick.AddListener(() => OnQuizAnswerSelected(button, index));

            if (quizAnswerButtons[i].GetComponent<AnswerButton>() == null)
            {
                quizAnswerButtons[i].gameObject.AddComponent<AnswerButton>();
            }
        }

        if (quizVoiceButton)
        {
            quizVoiceButton.onClick.AddListener(PlayQuizVoice);
        }

        List<string> tags = new List<string>();
        string learnCategoryId = PlayerPrefs.GetString("SelectedReviewTopic");
        tags.Add(learnCategoryId);
        int randomIndex = Random.Range(10, 15);
        StartReview(tags, randomIndex);
    }

    // Bắt đầu review với tags và số câu hỏi
    public void StartReview(List<string> tags, int questionCount)
    {
        selectedTags = tags;
        totalQuestions = questionCount;
        InitializeReview();
    }

    public void StartReview()
    {
        if (selectedTags.Count == 0)
        {
            Debug.LogWarning("No tags selected!");
            return;
        }

        InitializeReview();
    }

    void InitializeReview()
    {
        quizPanel.SetActive(false);
        matchingPanel.SetActive(false);
        dashboardPanel.SetActive(false);
        progressBar.value = 0;

        // Lấy vocabulary theo tags
        reviewVocabulary = vocabDatabase.GetVocabsByTags(selectedTags);

        if (reviewVocabulary.Count < numberOfAnswerChoices)
        {
            Debug.LogError($"Not enough vocabulary! Need at least {numberOfAnswerChoices} items.");
            return;
        }

        // Tạo danh sách câu hỏi (xen kẽ Quiz và Matching)
        GenerateReviewQuestions();

        currentQuestionIndex = 0;
        correctAnswers = 0;

        ShowCurrentQuestion();
        restartButton.gameObject.SetActive(false);
    }

    void GenerateReviewQuestions()
    {
        reviewQuestions = new List<ReviewQuestion>();

        // Shuffle vocabulary
        List<VocabItem> shuffled = reviewVocabulary.OrderBy(x => Random.value).ToList();

        // Tạo số lượng matching games (2-3 câu)
        int matchingGamesCount = Random.Range(2, 4); // 2 hoặc 3
        int quizCount = totalQuestions - matchingGamesCount;

        // Tạo list vị trí ngẫu nhiên cho matching games
        List<int> matchingPositions = new List<int>();
        while (matchingPositions.Count < matchingGamesCount)
        {
            int randomPos = Random.Range(0, totalQuestions);
            if (!matchingPositions.Contains(randomPos))
            {
                matchingPositions.Add(randomPos);
            }
        }

        int currentQuizIndex = 0;
        int currentMatchingIndex = 0;

        for (int i = 0; i < totalQuestions; i++)
        {
            ReviewQuestion reviewQ = new ReviewQuestion();

            if (matchingPositions.Contains(i))
            {
                // MatchingGame - lấy 3 cặp
                reviewQ.type = ReviewQuestionType.MatchingGame;
                reviewQ.matchingPairs = GenerateMatchingPairs(shuffled, currentMatchingIndex * 3, 3);
                currentMatchingIndex++;
            }
            else
            {
                // TextQuiz
                reviewQ.type = ReviewQuestionType.TextQuiz;
                reviewQ.quizQuestion = GenerateQuizQuestion(shuffled, currentQuizIndex);
                currentQuizIndex++;
            }

            reviewQuestions.Add(reviewQ);
        }
    }
    QuizQuestion GenerateQuizQuestion(List<VocabItem> vocab, int startIndex)
    {
        QuizQuestion question = new QuizQuestion();

        if (startIndex >= vocab.Count) startIndex = startIndex % vocab.Count;

        question.correctAnswer = vocab[startIndex];
        question.quizType = Random.value > 0.5f ? QuizType.TextQuiz : QuizType.ExampleQuiz;
        question.questionLanguage = Random.value > 0.5f ? QuestionLanguage.English : QuestionLanguage.Vietnamese;

        question.wrongAnswers = new List<VocabItem>();
        List<VocabItem> availableWrong = vocab.Where(v => v.id != question.correctAnswer.id).ToList();

        for (int j = 0; j < numberOfAnswerChoices - 1; j++)
        {
            if (availableWrong.Count > 0)
            {
                int randomIndex = Random.Range(0, availableWrong.Count);
                question.wrongAnswers.Add(availableWrong[randomIndex]);
                availableWrong.RemoveAt(randomIndex);
            }
        }

        return question;
    }

    List<MatchingPairData> GenerateMatchingPairs(List<VocabItem> vocab, int startIndex, int count)
    {
        List<MatchingPairData> pairs = new List<MatchingPairData>();

        for (int i = 0; i < count; i++)
        {
            int index = (startIndex + i) % vocab.Count;
            VocabItem item = vocab[index];
            Sprite sprite = assetManager.getSpriteAnimal(item.text.en.ToLower());
            MatchingPairData pair = new MatchingPairData
            {
                textEN = item.text.en,
                image = sprite, // Giả sử VocabItem có field image
                vocabItem = item
            };

            pairs.Add(pair);
        }

        return pairs;
    }

    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= reviewQuestions.Count)
        {
            ShowFinalResult();
            return;
        }

        currentReviewQuestion = reviewQuestions[currentQuestionIndex];
        questionAnswered = false;
        nextButton.interactable = false;

        if (currentReviewQuestion.type == ReviewQuestionType.TextQuiz)
        {
            ShowQuizQuestion();
        }
        else
        {
            ShowMatchingQuestion();
        }

        UpdateProgress();
    }

    // ============= QUIZ MODE =============
    void ShowQuizQuestion()
    {
        quizPanel.SetActive(true);
        matchingPanel.SetActive(false);

        QuizQuestion question = currentReviewQuestion.quizQuestion;

        // Tạo danh sách tất cả đáp án và shuffle
        List<VocabItem> allAnswers = new List<VocabItem>();
        allAnswers.Add(question.correctAnswer);
        allAnswers.AddRange(question.wrongAnswers);
        allAnswers = allAnswers.OrderBy(x => Random.value).ToList();

        quizQuestionText.text = question.GetQuestionText();

        // Hiển thị đáp án lên buttons
        for (int i = 0; i < quizAnswerButtons.Length; i++)
        {
            if (i < allAnswers.Count)
            {
                quizAnswerButtons[i].gameObject.SetActive(true);
                quizAnswerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = question.GetAnswerText(allAnswers[i]);
                quizAnswerButtons[i].interactable = true;
                quizAnswerButtons[i].GetComponent<AnswerButton>().vocabItem = allAnswers[i];
                quizAnswerButtons[i].GetComponent<Image>().color = new Color32(86, 107, 132, 255);
            }
            else
            {
                quizAnswerButtons[i].gameObject.SetActive(false);
            }
        }

        quizResultText.text = "";

        // Play voice
        string textToVoice = question.quizType == QuizType.TextQuiz ?
            question.correctAnswer.text.en : question.correctAnswer.example.en;
        audioManager.playVoiceWord(textToVoice);
    }

    void setQuestionText()
    {
        //quizQuestionText.text;
    }

    void OnQuizAnswerSelected(Button button, int buttonIndex)
    {
        if (questionAnswered) return;

        AnswerButton answerBtn = quizAnswerButtons[buttonIndex].GetComponent<AnswerButton>();
        QuizQuestion question = currentReviewQuestion.quizQuestion;

        bool isCorrect = answerBtn.vocabItem.id == question.correctAnswer.id;

        if (isCorrect)
        {
            correctAnswers++;
            quizResultText.text = "Đúng!";
            quizResultText.color = Color.green;
            button.GetComponent<Image>().color = Color.green;
            QuizAnswer quizAnswer = new QuizAnswer(question, isCorrect);
            listQuizAnswer[currentQuestionIndex] = quizAnswer;
        }
        else
        {
            quizResultText.text = "Sai!";
            quizResultText.color = Color.red;
            button.GetComponent<Image>().color = Color.red;
            QuizAnswer quizAnswer = new QuizAnswer(question, isCorrect);
            listQuizAnswer[currentQuestionIndex] = quizAnswer;
        }

        // Hiển thị đáp án đúng
        for (int i = 0; i < quizAnswerButtons.Length; i++)
        {
            AnswerButton ans = quizAnswerButtons[i].GetComponent<AnswerButton>();
            if (ans.vocabItem.id == question.correctAnswer.id)
            {
                quizAnswerButtons[i].GetComponent<Image>().color = Color.green;
            }
            quizAnswerButtons[i].interactable = false;
        }

        questionAnswered = true;
        nextButton.interactable = true;
    }

    void PlayQuizVoice()
    {
        if (currentReviewQuestion.type == ReviewQuestionType.TextQuiz)
        {
            QuizQuestion question = currentReviewQuestion.quizQuestion;
            string textToVoice = question.quizType == QuizType.TextQuiz ?
                question.correctAnswer.text.en : question.correctAnswer.example.en;
            audioManager.playVoiceWord(textToVoice);
        }
    }

    // ============= MATCHING MODE =============
    void ShowMatchingQuestion()
    {
        quizPanel.SetActive(false);
        matchingPanel.SetActive(true);

        matchingFeedbackText.text = "Chọn 3 cặp Text-Ảnh cho đúng!";
        matchingFeedbackText.color = Color.white;

        selectedTextButton = null;
        selectedImageButton = null;
        matchedPairsCount = 0;
        allMatchingButtons.Clear();

        // Clear old buttons
        foreach (Transform child in ButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        List<MatchingPairData> pairs = currentReviewQuestion.matchingPairs;

        // Shuffle texts and images separately
        List<MatchingPairData> shuffledTexts = pairs.OrderBy(x => Random.value).ToList();
        List<MatchingPairData> shuffledImages = pairs.OrderBy(x => Random.value).ToList();

        // Create text buttons
        for (int i = 0; i < shuffledTexts.Count; i++)
        {
            GameObject textBtn = Instantiate(textButtonPrefab, ButtonsContainer);
            MatchingButton matchBtn = textBtn.GetComponent<MatchingButton>();
            if (matchBtn == null) matchBtn = textBtn.AddComponent<MatchingButton>();
            matchBtn.Setup(shuffledTexts[i], true, this);
            allMatchingButtons.Add(matchBtn);
        }

        // Create image buttons
        for (int i = 0; i < shuffledImages.Count; i++)
        {
            GameObject imgBtn = Instantiate(imageButtonPrefab, ButtonsContainer);
            MatchingButton matchBtn = imgBtn.GetComponent<MatchingButton>();
            if (matchBtn == null) matchBtn = imgBtn.AddComponent<MatchingButton>();
            matchBtn.Setup(shuffledImages[i], false, this);
            allMatchingButtons.Add(matchBtn);
        }
    }

    public void OnMatchingButtonClicked(MatchingButton button)
    {
        if (questionAnswered) return;

        // Dừng audio trước khi phát audio mới
        if (audioManager != null && button.isTextButton)
        {
            audioManager.StopVoice();
        }

        if (button.isTextButton)
        {
            if (selectedTextButton != null)
            {
                selectedTextButton.Deselect();
            }
            selectedTextButton = button;
            button.Select();

            // Play audio
            audioManager.playVoiceWord(button.pairData.textEN);
        }
        else
        {
            if (selectedImageButton != null)
            {
                selectedImageButton.Deselect();
            }
            selectedImageButton = button;
            button.Select();
        }

        // Check if both selected
        if (selectedTextButton != null && selectedImageButton != null)
        {
            CheckMatch();
        }
    }

    void CheckMatch()
    {
        bool isMatch = selectedTextButton.pairData.textEN == selectedImageButton.pairData.textEN;

        if (isMatch)
        {
            // Correct match
            selectedTextButton.SetMatched();
            selectedImageButton.SetMatched();
            matchedPairsCount++;

            matchingFeedbackText.text = $"Đúng! Còn {3 - matchedPairsCount} cặp";
            matchingFeedbackText.color = Color.green;

            selectedTextButton = null;
            selectedImageButton = null;

            // Check if all matched
            if (matchedPairsCount >= 3)
            {
                matchingFeedbackText.text = "Hoàn thành!";
                correctAnswers++; // Tính 1 điểm cho matching game
                questionAnswered = true;
                nextButton.interactable = true;
            }
        }
        else
        {
            // Wrong match
            matchingFeedbackText.text = "Sai rồi! Chọn lại nhé";
            matchingFeedbackText.color = Color.red;

            selectedTextButton.Deselect();
            selectedImageButton.Deselect();
            selectedTextButton = null;
            selectedImageButton = null;
        }
    }

    // ============= NAVIGATION =============
    void LoadNextQuestion()
    {
        UpdateProgressBar();
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    void UpdateProgress()
    {
        //progressText.text = $"Câu {currentQuestionIndex + 1}/{reviewQuestions.Count}";
    }

    void UpdateProgressBar()
    {
        float incrementValue = progressBar.maxValue / reviewQuestions.Count;
        progressBar.value += incrementValue;
    }

    void ShowFinalResult()
    {
        quizPanel.SetActive(false);
        matchingPanel.SetActive(false);
        dashboardPanel.SetActive(true);

        float percentage = (float)correctAnswers / reviewQuestions.Count * 100f;

        string grade = "";
        if (percentage >= 90) grade = "Xuất sắc!";
        else if (percentage >= 70) grade = "Tốt lắm";
        else if (percentage >= 50) grade = "Cố gắng";
        else grade = "Cần luyện tập thêm";

        finalScoreText.text = $"Tổng điểm: {correctAnswers}/{reviewQuestions.Count}\n{percentage:F1}%";
        finalGradeText.text = grade;
        finalGradeText.color = percentage >= 70 ? Color.green : (percentage >= 50 ? Color.yellow : Color.red);

        nextButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(true);

        InitializeDashboard();
    }

    void InitializeDashboard()
    {
        // Clear old items
        foreach (Transform child in dashboardLayoutTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (var keyValuePair in listQuizAnswer)
        {
            GameObject item = Instantiate(reviewAnswerItemPrefab, dashboardLayoutTransform);
            item.SetActive(true);
            int index = keyValuePair.Key + 1;
            item.GetComponent<QuizAnswerItem>().SetQuizData(index, keyValuePair.Value);
        }
        string learnCategoryId = PlayerPrefs.GetString("SelectedReviewTopic");
        FirebaseDatabaseManager.Instance.SaveProgress(learnCategoryId, "review", true);
    }

    public void RestartReview()
    {
        nextButton.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(false);

        // Clear dashboard
        foreach (Transform child in dashboardLayoutTransform)
        {
            Destroy(child.gameObject);
        }

        InitializeReview();
    }

    public void onClickHome()
    {
        SceneManager.LoadScene("HomeScene");
    }
}
