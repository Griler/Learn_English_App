using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "UserProfileSO", menuName = "Data/UserProfileSO")]
public class UserProfileSO : ScriptableObject
{
    // --- PHẦN 1: USER INFO ---
    public UserInfoData userInfo;
    public event Action<UserInfoData> OnUserInfoChanged; // Sự kiện A

    public void UpdateUserInfo(UserInfoData newData)
    {
        userInfo = newData;
        OnUserInfoChanged?.Invoke(userInfo); // Chỉ gọi ai quan tâm đến Info
    }

    // --- PHẦN 2: FRIEND LIST ---
    public List<FriendData> friendList = new List<FriendData>();
    public event Action<List<FriendData>> OnFriendListChanged; // Sự kiện B

    public void UpdateFriendList(List<FriendData> newList)
    {
        friendList = newList;
        OnFriendListChanged?.Invoke(friendList); // Chỉ gọi ai quan tâm đến Friend
    }
}