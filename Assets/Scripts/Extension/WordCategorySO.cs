using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class WordData
{
    public int id;
    public string name_en;
    public string name_vi;
    public string image;
    
    [NonSerialized]
    public Sprite sprite;
    [NonSerialized]
    public AudioClip audioClip;
}
[CreateAssetMenu(fileName = "New Container", menuName = "Game Data/AnimalCategorySO")]
[System.Serializable]
public class WordCategorySO : ScriptableObject
{
    public string categoryName;
    public List<WordData> animals;
}

[Serializable]
public class WordDatabase
{
    [FormerlySerializedAs("animals")] public WordCategorySO words;
}