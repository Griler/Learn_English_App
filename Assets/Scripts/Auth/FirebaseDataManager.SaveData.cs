using System;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Newtonsoft.Json;
using UnityEditor;
using DataSnapshot = Firebase.Database.DataSnapshot;

public partial class FirebaseDatabaseManager : MonoBehaviour
{
    public void SaveUserProgress(string mainTopic, string subTopicCompleted, List<string> allSubTopicsInThisCategory)
    {
        DatabaseReference userProgressRef = dbReference.Child("users").Child(currentUser.UserId).Child("learning_progress/vocab_topics").Child(mainTopic);
        // 1. Đánh dấu Subtopic này là đã học xong (true)
        userProgressRef.Child(subTopicCompleted).SetValueAsync(true).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi khi lưu tiến độ subtopic: " + task.Exception);
            }
            else
            {
                Debug.Log($"Đã lưu xong subtopic: {subTopicCompleted}");
                CheckAndMarkParentTopic(userProgressRef, allSubTopicsInThisCategory);
            }
        });
    }

    private void CheckAndMarkParentTopic(DatabaseReference mainTopicRef, List<string> allSubTopics)
    {
        mainTopicRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                bool isAllDone = true;
                foreach (string subName in allSubTopics)
                {
                    if (!snapshot.HasChild(subName))
                    {
                        isAllDone = false;
                        break;
                    }
                }
                if (isAllDone)
                {
                    Debug.Log("Chúc mừng! Bạn đã hoàn thành toàn bộ chủ đề lớn.");
                    mainTopicRef.Child("isCompleted").SetValueAsync(true);
                }
            }
        });
    }
    
}