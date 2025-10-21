
using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "GrammarData", menuName = "EnglishLearning/Grammar Data")]
[System.Serializable]
public class GrammarData : ScriptableObject
{
    public string grammarPointID;
    public string rule;
    public string name;
    public string description;
    public List<GrammarFlashcardExmpale> examples;
    public List<GrammarFlashcardExercise> miniExercises;
}