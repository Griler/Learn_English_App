using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class SubTopicSelectManager : MonoBehaviour
{
    [SerializeField] Transform contentParent;
    [SerializeField] GameObject subTopicButtonPrefab;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text statusText;

    void OnEnable()
    {
        string mainTopic = PlayerPrefs.GetString("SelectedMainTopic");
        if (string.IsNullOrEmpty(mainTopic))
        {
            SceneManager.LoadScene("HomeScene");
            return;
        }

        titleText.text = mainTopic.ToUpper();
        LoadSubTopics(mainTopic);
    }

    void LoadSubTopics(string mainTopic)
    {
        statusText.text = "Loading...";
        string path = "vocab_topics/" +  mainTopic;
        FirebaseDatabase.DefaultInstance
            .GetReference("vocab_topics")
            .Child(mainTopic)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    List<string> subTopics = new List<string>();
                    foreach (var child in snapshot.Children)
                    {
                        subTopics.Add(child.Key);
                    }
                    Populate(subTopics);
                    statusText.text = "";
                }
                else
                {
                    statusText.text = "Load failed!";
                }
            });
    }

    void Populate(List<string> subTopics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (string sub in subTopics)
        {
            GameObject lessonItem = Instantiate(subTopicButtonPrefab, contentParent);
            lessonItem.GetComponent<LessonItem>().setData(topicName:sub);
            lessonItem.GetComponentInChildren<TMP_Text>().text = sub;
            lessonItem.GetComponentInChildren<Button>().onClick.AddListener(() => OnSubTopicSelected(sub));
        }
    }

    void OnSubTopicSelected(string subTopicName)
    {
        PlayerPrefs.SetString("SelectedSubTopic", subTopicName);
        SceneManager.LoadScene("FlashCardScene");
    }
}
