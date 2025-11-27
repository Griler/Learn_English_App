using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class WordData
{
    public int id;
    public string name;
    public string nameEn;
    public string nameVi;
    public string image;
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