using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

// ============== DATA STRUCTURES ==============
[Serializable]
public class VocabText
{
    public string en;
    public string vi;
}

[Serializable]
public class VocabExample
{
    public string en;
    public string vi;
}

[Serializable]
public class VocabItem
{
    public string id;
    public VocabText text;
    public VocabExample example;
    public List<string> tags;
}

// ============== SCRIPTABLE OBJECT ==============
[CreateAssetMenu(fileName = "VocabularyDatabase", menuName = "Learning/Vocabulary Database")]
public class VocabularyDatabase : ScriptableObject
{
    public List<VocabItem> vocabularyItems = new List<VocabItem>();
    
    // Lấy tất cả tags unique
    public List<string> GetAllTags()
    {
        HashSet<string> allTags = new HashSet<string>();
        foreach (var item in vocabularyItems)
        {
            foreach (var tag in item.tags)
            {
                allTags.Add(tag);
            }
        }
        return new List<string>(allTags);
    }
    
    // Lọc vocab theo tags
    public List<VocabItem> GetVocabsByTags(List<string> selectedTags)
    {
        List<VocabItem> filtered = new List<VocabItem>();
        
        foreach (var item in vocabularyItems)
        {
            bool hasMatchingTag = false;
            foreach (var tag in item.tags)
            {
                if (selectedTags.Contains(tag))
                {
                    hasMatchingTag = true;
                    break;
                }
            }
            
            if (hasMatchingTag)
            {
                filtered.Add(item);
            }
        }
        
        return filtered;
    }
    
    // Lọc vocab theo một tag cụ thể
    public List<VocabItem> GetVocabsByTag(string tag)
    {
        List<VocabItem> filtered = new List<VocabItem>();
        
        foreach (var item in vocabularyItems)
        {
            if (item.tags.Contains(tag))
            {
                filtered.Add(item);
            }
        }
        
        return filtered;
    }
    
    // Lọc vocab theo nhiều tags (phải có tất cả tags)
    public List<VocabItem> GetVocabsByAllTags(List<string> requiredTags)
    {
        List<VocabItem> filtered = new List<VocabItem>();
        
        foreach (var item in vocabularyItems)
        {
            bool hasAllTags = true;
            foreach (var tag in requiredTags)
            {
                if (!item.tags.Contains(tag))
                {
                    hasAllTags = false;
                    break;
                }
            }
            
            if (hasAllTags)
            {
                filtered.Add(item);
            }
        }
        
        return filtered;
    }
}

// ============== JSON WRAPPER (for parsing) ==============
[Serializable]
public class JsonVocabItem
{
    public VocabText text;
    public VocabExample example;
    public List<string> tags;
}

[Serializable]
public class JsonVocabWrapper
{
    public string li_00001;
    public string li_00002;
    // ... add all IDs
}

// ============== EDITOR TOOL TO IMPORT JSON ==============
#if UNITY_EDITOR
public class VocabularyImporter : EditorWindow
{
    private TextAsset jsonFile;
    private VocabularyDatabase database;
    
    [MenuItem("Tools/Import Vocabulary from JSON")]
    static void ShowWindow()
    {
        GetWindow<VocabularyImporter>("Vocab Importer");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Import Vocabulary from JSON", EditorStyles.boldLabel);
        
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        database = (VocabularyDatabase)EditorGUILayout.ObjectField("Database", database, typeof(VocabularyDatabase), false);
        
        if (GUILayout.Button("Import"))
        {
            if (jsonFile != null && database != null)
            {
                ImportFromJson();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign both JSON file and Database!", "OK");
            }
        }
        
        if (GUILayout.Button("Create New Database"))
        {
            CreateNewDatabase();
        }
    }
    
    void ImportFromJson()
    {
        try
        {
            database.vocabularyItems.Clear();
            
            string jsonText = jsonFile.text;
            
            // Parse JSON manually vì structure đặc biệt
            ParseJsonManually(jsonText);
            
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Success", 
                $"Imported {database.vocabularyItems.Count} vocabulary items!", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to import: {e.Message}", "OK");
        }
    }
    
    void ParseJsonManually(string jsonText)
    {
        // Remove whitespace
        jsonText = jsonText.Trim();
        
        // Remove outer braces
        if (jsonText.StartsWith("{")) jsonText = jsonText.Substring(1);
        if (jsonText.EndsWith("}")) jsonText = jsonText.Substring(0, jsonText.Length - 1);
        
        // Split by "li_" to find each item
        string[] items = jsonText.Split(new string[] { "\"li_" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string itemStr in items)
        {
            if (string.IsNullOrEmpty(itemStr.Trim())) continue;
            
            try
            {
                // Extract ID
                int idEnd = itemStr.IndexOf("\"");
                if (idEnd < 0) continue;
                string id = "li_" + itemStr.Substring(0, idEnd);
                
                // Find the object content
                int objStart = itemStr.IndexOf("{");
                int objEnd = FindMatchingBrace(itemStr, objStart);
                
                if (objStart < 0 || objEnd < 0) continue;
                
                string objStr = itemStr.Substring(objStart, objEnd - objStart + 1);
                
                // Parse the object
                VocabItem item = ParseVocabItem(id, objStr);
                if (item != null)
                {
                    database.vocabularyItems.Add(item);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse item: {e.Message}");
            }
        }
    }
    
    int FindMatchingBrace(string str, int start)
    {
        int count = 0;
        for (int i = start; i < str.Length; i++)
        {
            if (str[i] == '{') count++;
            else if (str[i] == '}')
            {
                count--;
                if (count == 0) return i;
            }
        }
        return -1;
    }
    
    VocabItem ParseVocabItem(string id, string json)
    {
        VocabItem item = new VocabItem();
        item.id = id;
        item.text = new VocabText();
        item.example = new VocabExample();
        item.tags = new List<string>();
        
        // Parse text.en
        item.text.en = ExtractStringValue(json, "\"text\"", "\"en\"");
        item.text.vi = ExtractStringValue(json, "\"text\"", "\"vi\"");
        
        // Parse example
        item.example.en = ExtractStringValue(json, "\"example\"", "\"en\"");
        item.example.vi = ExtractStringValue(json, "\"example\"", "\"vi\"");
        
        // Parse tags
        item.tags = ExtractTags(json);
        
        return item;
    }
    
    string ExtractStringValue(string json, string section, string key)
    {
        int sectionStart = json.IndexOf(section);
        if (sectionStart < 0) return "";
        
        int keyPos = json.IndexOf(key, sectionStart);
        if (keyPos < 0) return "";
        
        int valueStart = json.IndexOf("\"", keyPos + key.Length + 1);
        if (valueStart < 0) return "";
        
        valueStart++;
        int valueEnd = json.IndexOf("\"", valueStart);
        if (valueEnd < 0) return "";
        
        return json.Substring(valueStart, valueEnd - valueStart);
    }
    
    List<string> ExtractTags(string json)
    {
        List<string> tags = new List<string>();
        
        int tagsStart = json.IndexOf("\"tags\"");
        if (tagsStart < 0) return tags;
        
        int arrayStart = json.IndexOf("[", tagsStart);
        int arrayEnd = json.IndexOf("]", arrayStart);
        
        if (arrayStart < 0 || arrayEnd < 0) return tags;
        
        string tagsStr = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
        string[] tagArray = tagsStr.Split(',');
        
        foreach (string tag in tagArray)
        {
            string cleanTag = tag.Trim().Trim('"');
            if (!string.IsNullOrEmpty(cleanTag))
            {
                tags.Add(cleanTag);
            }
        }
        
        return tags;
    }
    
    void CreateNewDatabase()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Vocabulary Database",
            "VocabularyDatabase",
            "asset",
            "Create a new vocabulary database"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            VocabularyDatabase newDb = CreateInstance<VocabularyDatabase>();
            AssetDatabase.CreateAsset(newDb, path);
            AssetDatabase.SaveAssets();
            
            database = newDb;
            EditorUtility.DisplayDialog("Success", "Database created!", "OK");
        }
    }
}
#endif