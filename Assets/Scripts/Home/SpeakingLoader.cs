using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpeakingLoader : MonoBehaviour
{
    [SerializeField]private EnglishData data;
    [SerializeField]private List<string> key;
    [SerializeField]private List<Sprite> sprite;
    [SerializeField]private GameObject item;
    [SerializeField]private Transform container;
    Dictionary<string, int> topicIndexDict = new Dictionary<string, int>();

    IEnumerator Start()
    {
        while (!FirebaseDatabaseManager.IsReady)
        {
            Debug.Log("⏳ Waiting for Firebase init...");
            yield return null;
        }

        // Gọi load data khi Firebase đã sẵn sàng
        LoadTopicsFromFirebase();
        
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void LoadTopicsFromFirebase()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("speaking")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    int index = 0;
                    foreach (var child in snapshot.Children)
                    {
                       key.Add(child.Key);
                       string keyDic = child.Key;  // ví dụ: "greetings"
                       topicIndexDict[keyDic] = index; 
                       index++;
                    }
                    string json = snapshot.GetRawJsonValue();
                    Debug.Log("Loaded JSON:\n" + json);

                    // Parse JSON to class
                     data = JsonUtility.FromJson<EnglishData>(json);
                    
                    Debug.Log("First greeting: " + data.greetings[0].en);
                    
                    LoadItem();
                }
            });
    }

    void LoadItem()
    {

        foreach (Transform c in container) Destroy(c.gameObject);
        for (int i = 0; i < key.Count; i++)
        {
            GameObject go = Instantiate(item, container);
            go.GetComponent<SpeakingItem>().setImage(sprite[i]);
            string[] parts = key[i].Split('_');
            go.GetComponent<SpeakingItem>().setName(parts[0]);
            string currentKey = key[i];
            go.GetComponent<SpeakingItem>().setOnClickButton(() => OnTopicClicked(currentKey));
            //go.GetComponent<SpeakingItem>().on
        }
    }

    public void OnTopicClicked(string topicKey)
    {
        PlayerPrefs.SetString("CurrentSpeakingTopic", topicKey);
        SceneManager.LoadScene("SpeakingScene");
    }
}
[System.Serializable]
public class SentenceItem
{
    public string vn;
    public string en;
}

[System.Serializable]
public class EnglishData
{
    public List<SentenceItem> greetings;
    public List<SentenceItem> introductions;
    public List<SentenceItem> daily_conversation;
    public List<SentenceItem> shopping;
    public List<SentenceItem> travel;
    public List<SentenceItem> restaurant;
    public List<SentenceItem> feelings;
}