using System.Collections.Generic;

public class QuizQuestion
{
    public VocabItem correctAnswer;
    public List<VocabItem> wrongAnswers;
    public QuizType quizType;
    public QuestionLanguage questionLanguage;
    
    // Ngôn ngữ trả lời luôn ngược với câu hỏi
    public QuestionLanguage AnswerLanguage
    {
        get
        {
            return questionLanguage == QuestionLanguage.English 
                ? QuestionLanguage.Vietnamese 
                : QuestionLanguage.English;
        }
    }
    
    public string GetQuestionText()
    {
        if (quizType == QuizType.TextQuiz)
        {
            return questionLanguage == QuestionLanguage.English 
                ? correctAnswer.text.en 
                : correctAnswer.text.vi;
        }
        else // ExampleQuiz
        {
            return questionLanguage == QuestionLanguage.English 
                ? correctAnswer.example.en 
                : correctAnswer.example.vi;
        }
    }
    
    public string GetAnswerText(VocabItem item)
    {
        if (quizType == QuizType.TextQuiz)
        {
            return AnswerLanguage == QuestionLanguage.English 
                ? item.text.en 
                : item.text.vi;
        }
        else // ExampleQuiz
        {
            return AnswerLanguage == QuestionLanguage.English 
                ? item.example.en 
                : item.example.vi;
        }
    }
}