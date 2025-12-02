using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class FriendListPanel : MonoBehaviour
{
    public Transform contentContainer;
    public GameObject friendItemPrefab;
    
    public TextMeshProUGUI resultText; 
    
    private DatabaseReference _dbRef;

    public async void OnShow()
    {
        await FirebaseDatabaseManager.Instance.InitializeFirebase();
        _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        LoadFriendList();
    }
    
     public void LoadFriendList()
    {
        // Xóa sạch danh sách cũ trên UI để tránh trùng lặp
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        string currentUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        string pathListID = $"users/{currentUserId}/friend/userId";

        _dbRef.Child(pathListID).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Lỗi khi lấy danh sách ID bạn bè: " + task.Exception);
                resultText.gameObject.SetActive(true);
                string error = "Lỗi khi lấy danh sách ID bạn bè: " + currentUserId;  
                resultText.text = error;
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                List<string> friendIdList = new List<String>();
                if (snapshot.Exists && snapshot.HasChildren)
                {
                    resultText.gameObject.SetActive(false);
                    
                    foreach (DataSnapshot childNode in snapshot.Children)
                    {
                        string friendId = childNode.Value.ToString();
                        friendIdList.Add(friendId);
                    }
                }
                else
                {
                    Debug.Log("User này chưa có bạn bè nào.");
                    resultText.gameObject.SetActive(true);
                    string text = "You Has No Friends";  
                    resultText.text = text;
                }
                
                initUI(friendIdList);
            }
        });
    }

    void initUI(List<string> friendIdList)
    {
        foreach (string friendId in friendIdList)
        {
            SpawnFriendItem(friendId);
        }
    }
    
    void SpawnFriendItem(string friendId)
    {
        
        if (friendItemPrefab == null || contentContainer == null) return;

        FriendActionService.Instance.FetchOtherUserInfo(friendId, 
            (info) => {
                GameObject newItem = Instantiate(friendItemPrefab, contentContainer);
                FriendItemUI itemScript = newItem.GetComponent<FriendItemUI>();
                if (itemScript != null)
                {
                    itemScript.SetupUI(info, friendId);
                }
            },
            (error) => {
                Debug.LogError("Lỗi: " + friendId);
            }
        );
    }
}