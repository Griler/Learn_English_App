
using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[System.Serializable]
public class GrammarData
{
    public string grammarPointID;
    public string rule;
    //public string name;
    public string description;
    public List<GrammarFlashcardExmpale> examples;
    public List<GrammarFlashcardExercise> miniExercises;
    
    public GrammarData()
    {
        examples = new List<GrammarFlashcardExmpale>();
        miniExercises = new List<GrammarFlashcardExercise>();
    }
}