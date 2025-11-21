using System;
using System.Collections.Generic;

public class GameData
{
    // Tên biến phải KHỚP Y HỆT key trong JSON: "topics"
    public List<TopicData> topics; 
}

[Serializable]
public class TopicData
{
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