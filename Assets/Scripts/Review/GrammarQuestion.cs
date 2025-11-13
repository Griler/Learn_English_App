using System;
using System.Collections.Generic;

[System.Serializable]
public class GrammarQuestion
{
    public int id;
    public string question;
    public List<string> options; // Dùng List<string> cho mảng JSON
    public string answer;
    public string grammarName;
    public string grammarPoint;
}



