using System;
using System.Collections.Generic;
using UnityEngine;

public class VocabularyDataManager : BaseCode
{
    public static VocabularyDataManager Instance;
    private string path= "AnimalSO/Animals";

    [SerializeField] private AnimalDatabase database;
    [SerializeField] private ContainerSO dataContainer;
    
    public List<AnimalData> allAnimals = new List<AnimalData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadData()
    {
        dataContainer = ScriptableObject.Instantiate(Resources.Load<ContainerSO>(path));
        // Parse JSON
        if (dataContainer != null)
        {
            // Combine all animals
            allAnimals.AddRange(dataContainer.FindItemByCategoryName("Pet").animals);
            allAnimals.AddRange(dataContainer.FindItemByCategoryName("Farm Animal").animals);
            allAnimals.AddRange(dataContainer.FindItemByCategoryName("Sea Animal").animals);
            allAnimals.AddRange(dataContainer.FindItemByCategoryName("Wild Animal").animals);
            
            LinkResources(dataContainer.FindItemByCategoryName("Pet").animals);
            LinkResources(dataContainer.FindItemByCategoryName("Farm Animal").animals);
            LinkResources(dataContainer.FindItemByCategoryName("Sea Animal").animals);
            LinkResources(dataContainer.FindItemByCategoryName("Wild Animal").animals);
            
            Debug.Log($"Loaded {allAnimals.Count} animals");
        }
        else
        {
            Debug.LogError("JSON file not assigned!");
        }
    }
    
    void LinkResources(List<AnimalData> animals)
    {
        foreach (var animal in animals)
        {
            // Link sprite
            string spriteName = config.formatSpriteName(animal.name_en);
            if (spriteName != null)
            {
                animal.sprite = assetManager.getSprite(spriteName);
            }
            else
            {
            }
            
            // Link audio
            // if (audioDict.ContainsKey(spriteName))
            // {
            //     animal.audioClip = audioDict[spriteName];
            // }
        }
    }
    
    // Get animals by category
    public List<AnimalData> GetAnimalsByCategory(string category)
    {
        switch (category)
        {
            case "Pet": return dataContainer.FindItemByCategoryName("Pet").animals;
            case "Farm Animal": return dataContainer.FindItemByCategoryName("Farm Animal").animals;
            case "Wild Animal": return dataContainer.FindItemByCategoryName("Wild Animal").animals;
            case "Sea Animal": return dataContainer.FindItemByCategoryName("Sea Animal").animals;
            default: return allAnimals;
        }
    }
    
    // Get random animals
    public List<AnimalData> GetRandomAnimals(int count)
    {
        List<AnimalData> result = new List<AnimalData>();
        List<AnimalData> temp = new List<AnimalData>(allAnimals);
        
        for (int i = 0; i < Mathf.Min(count, temp.Count); i++)
        {
            int rand = UnityEngine.Random.Range(0, temp.Count);
            result.Add(temp[rand]);
            temp.RemoveAt(rand);
        }
        
        return result;
    }
    
    // Get random animals from category
    public List<AnimalData> GetRandomAnimalsFromCategory(string category, int count)
    {
        List<AnimalData> categoryAnimals = GetAnimalsByCategory(category);
        List<AnimalData> result = new List<AnimalData>();
        List<AnimalData> temp = new List<AnimalData>(categoryAnimals);
        
        for (int i = 0; i < Mathf.Min(count, temp.Count); i++)
        {
            int rand = UnityEngine.Random.Range(0, temp.Count);
            result.Add(temp[rand]);
            temp.RemoveAt(rand);
        }
        
        return result;
    }
}