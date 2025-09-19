using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class AnimalImporterWindow : EditorWindow
{
    private TextAsset jsonFile;
    private string saveFolder = "Assets/AnimalSO";

    [MenuItem("Tools/Animal Importer")]
    public static void ShowWindow()
    {
        GetWindow<AnimalImporterWindow>("Animal Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Import Animal JSON → Multiple SO", EditorStyles.boldLabel);

        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        saveFolder = EditorGUILayout.TextField("Save Folder", saveFolder);

        if (GUILayout.Button("Convert & Save"))
        {
            if (jsonFile == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a JSON file!", "OK");
                return;
            }

            CreateSOFromJson(jsonFile.text, saveFolder);
        }
    }

    void CreateSOFromJson(string json, string folder)
    {
        // Parse JSON
        
        AnimalWrapper wrapper = JsonConvert.DeserializeObject<AnimalWrapper>(json);
        if (wrapper == null || wrapper.animals == null)
        {
            Debug.LogError("JSON Parse Failed!");
            return;
        }

        // Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        // Tạo từng SO theo category
        foreach (var category in wrapper.animals.Keys)
        {
            AnimalCategorySO so = ScriptableObject.CreateInstance<AnimalCategorySO>();
            so.categoryName = category;
            so.animals = wrapper.animals[category];

            string safeName = category.Replace(" ", "_");
            string path = $"{folder}/{safeName}.asset";

            AssetDatabase.CreateAsset(so, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", "All Animal Categories saved to: " + folder, "OK");
    }
}

#region Data Model
// JSON structure
[System.Serializable]
public class AnimalWrapper
{
    public Dictionary<string, List<Animal>> animals;
}

[System.Serializable]
public class Animal
{
    public int id;
    public string name_en;
    public string name_vi;
    public string image;
}

// SO class cho từng category
public class AnimalCategorySO : ScriptableObject
{
    public string categoryName;
    public List<Animal> animals;
}
#endregion
