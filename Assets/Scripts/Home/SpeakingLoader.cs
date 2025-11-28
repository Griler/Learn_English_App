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

    void Start()
    {
        LoadData();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void LoadData()
    {
        StartCoroutine(ApiController.Instance.GetSpeakingCategories(LoadItem));
    }

    void LoadItem(List<SpeakingCategory> categories)
    {

        foreach (Transform c in container) Destroy(c.gameObject);
        for (int i = 0; i < categories.Count; i++)
        {
            GameObject go = Instantiate(item, container);
            go.GetComponent<SpeakingItem>().setImage(sprite[i]);
            string[] parts = categories[i].name.Split('_');
            go.GetComponent<SpeakingItem>().setName(parts[0]);
            int categoryId = categories[i].id;
            go.GetComponent<SpeakingItem>().setOnClickButton(() => OnTopicClicked(categoryId));
            //go.GetComponent<SpeakingItem>().on
        }
    }

    public void OnTopicClicked(int categoryId)
    {
        PlayerPrefs.SetInt("SelectedSpeakingTopic", categoryId);
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