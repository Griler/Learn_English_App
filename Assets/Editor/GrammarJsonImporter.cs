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
        }
    }

    
}