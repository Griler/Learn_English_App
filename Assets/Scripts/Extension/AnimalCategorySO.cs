using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Animal
{
    public int id;
    public string name_en;
    public string name_vi;
    public string image;
}
[CreateAssetMenu(fileName = "New Container", menuName = "Game Data/AnimalCategorySO")]
[System.Serializable]
public class AnimalCategorySO : ScriptableObject
{
    public string categoryName;
    public List<Animal> animals;
}
