using System;
using System.Collections.Generic;

[System.Serializable]
public class GrammarFlashcard{
    public string grammarPointID;
    public string ruleText;
}
[System.Serializable]
public class GrammarFlashcardExmpale : GrammarFlashcard
{
   
    public string sentence;
    public string translation;
    public string conjugatedVerb;
    
}

[System.Serializable]
public class GrammarFlashcardExercise : GrammarFlashcard
{
    public string question;
    public string answer;
    public string difficultyLevel;

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
    
    public GrammarFlashcardExercise()
    {
        interval = 0;
        easeFactor = 2.5f;
        nextReviewDate = DateTime.UtcNow;
    }
}


[System.Serializable]
public class GrammarTopic
{
    public string grammarPointID;
    public string rule;
    public string description;
    public List<GrammarFlashcardExmpale> examples;
    public List<GrammarFlashcardExercise> miniExercises;
}