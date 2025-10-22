using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public class GrammarJsonImporter : EditorWindow
{
    private TextAsset jsonFile;

    [MenuItem("Tools/Import Grammar JSON")]
    public static void ShowWindow()
    {
        GetWindow<GrammarJsonImporter>("Import Grammar JSON");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import Grammar JSON to ScriptableObject", EditorStyles.boldLabel);
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);

        if (jsonFile != null && GUILayout.Button("Import"))
        {
            ImportJson(jsonFile);
        }
    }

    private void ImportJson(TextAsset jsonFile)
    {
        string json = jsonFile.text;
        
        // Deserialize sang class tạm
        GrammarData container = ScriptableObject.CreateInstance<GrammarData>(); 
        container = JsonConvert.DeserializeObject<GrammarData>(json);
        Debug.Log(container);
        if (container == null)
        {
            Debug.LogError("❌ Failed to parse JSON!");
            return;
        }

        // Tạo instance ScriptableObject
        GrammarData data = ScriptableObject.CreateInstance<GrammarData>();

        // Gán giá trị
        data.grammarPointID = container.grammarPointID;
        data.name = container.name;
        data.rule = container.rule;
        data.description = container.description;
        data.description = container.description;
        data.examples = new System.Collections.Generic.List<GrammarFlashcardExmpale>(container.examples);
        for (var i = 0; i < data.examples.Count; i++)
        {
            data.examples[i].grammarPointID = container.grammarPointID;
            data.examples[i].ruleText = container.rule;
        }
        data.miniExercises = new System.Collections.Generic.List<GrammarFlashcardExercise>(container.miniExercises);
        for (var i = 0; i < data.miniExercises.Count; i++)
        {
            data.miniExercises[i].grammarPointID = container.grammarPointID;
            data.miniExercises[i].grammarPointID = container.rule;
        }
        // Lưu asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Grammar Data",
            data.grammarPointID + ".asset",
            "asset",
            "Choose location to save GrammarData asset"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;
            Debug.Log("✅ Successfully imported: " + data.name);
        }
    }
}