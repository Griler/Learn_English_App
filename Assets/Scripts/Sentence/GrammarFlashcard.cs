using System;

[System.Serializable]
public class GrammarFlashcardExmpale
{
   
    public string sentence;
    public string translation;
    public string grammarPointID;
}

[System.Serializable]
public class GrammarFlashcardExercise 
{
    public string question;
    public string answer;
    public string difficultyLevel;
    public string grammarPointID;

    // Thuộc tính cho thuật toán Spaced Repetition
    public int interval; // Khoảng cách (ngày) cho lần ôn tập tiếp theo
    public float easeFactor; // Hệ số dễ, mặc định là 2.5f
    public DateTime nextReviewDate;

    public GrammarFlashcardExercise(string question, string answer, string difficultyLevel, string grammarPointID)
    {
        this.question = question;
        this.answer = answer;
        this.difficultyLevel = difficultyLevel;
        this.grammarPointID = grammarPointID;
        
        interval = 0;
        easeFactor = 2.5f;
        nextReviewDate = DateTime.UtcNow;
    }
}