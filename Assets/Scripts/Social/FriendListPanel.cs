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
    public UserProfileSO UserProfileSo;
    public TextMeshProUGUI resultText; 
    
    private DatabaseReference _dbRef;

    public void OnShow()
    {
        if (FirebaseDatabase.DefaultInstance == null) 
        {
            Debug.LogError("Firebase chưa được khởi tạo!");
            return;
        }

        _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        LoadFriendList();
    }

    private void OnEnable()
    {
        UserProfileSo.OnFriendListChanged += LoadFriendList;
    }

    private void OnDisable()
    {
        UserProfileSo.OnFriendListChanged -= LoadFriendList;
    }

    private void ClearCurrentList()
    {
        foreach (Transform child in contentContainer)
        {
            // FIX QUAN TRỌNG: Tắt đi trước để Layout Group không tính toán lại -> Tránh lỗi MissingReference
            child.gameObject.SetActive(false); 
            Destroy(child.gameObject);
        }
    }
    
     public void LoadFriendList()
    {
        // Xóa sạch danh sách cũ trên UI để tránh trùng lặp
        ClearCurrentList();
        
        string currentUserId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        string pathListID = $"users/{currentUserId}/friend/userId";

        Debug.LogError(pathListID);
        
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
                    Debug.Log(snapshot.ToString());
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
                    itemScript.SetupUI(info, friendId, LoadFriendList);
                }
            },
            (error) => {
                Debug.LogError("Lỗi: " + friendId);
            }
        );
    }
}