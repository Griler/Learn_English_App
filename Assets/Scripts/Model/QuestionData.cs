using System;

[Serializable]
public class QuestionData
{
    public int id;
    public string questionText;
    public string[] answers; // 4 đáp án
    public int correctAnswerIdx; // Index đáp án đúng (0-3)
}