using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AnimalData
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
public class AnimalCategorySO : ScriptableObject
{
    public string categoryName;
    public List<AnimalData> animals;
}

[Serializable]
public class AnimalDatabase
{
    public AnimalCategorySO animals;
}