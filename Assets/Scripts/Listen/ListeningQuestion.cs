using System;
using System.Collections.Generic;


[Serializable]
public class ListenCategory
{
    public int id;
    public string topicName;
}

// Class câu hỏi (giống cái bạn đang dùng, đảm bảo nó Serializable)
[Serializable]
public class ListeningQuestion
{
    public string textToSpeak;
    public string correctAnswer;
    public string wrongAnswers;
}

[System.Serializable]
public class ListenAnswer
{
    public string correctAws;
    public bool isCorrect;
}