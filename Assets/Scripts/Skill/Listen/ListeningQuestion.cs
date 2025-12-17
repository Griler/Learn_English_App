using System;
using System.Collections.Generic;



[Serializable]
public class ListCategory
{
    // Tên biến phải KHỚP Y HỆT key trong JSON: "topics"
    public List<ListenCategory> topics; 
}

[Serializable]
public class ListenCategory
{
    public int id;
    public string topicName;
    public List<ListeningQuestion> questions;

}

// Class câu hỏi (giống cái bạn đang dùng, đảm bảo nó Serializable)
[Serializable]
public class ListeningQuestion
{
    public string textToSpeak;
    public string correctAnswer;
    public List<string> wrongAnswers;
}

[System.Serializable]
public class ListenAnswer
{
    public string correctAws;
    public bool isCorrect;
}