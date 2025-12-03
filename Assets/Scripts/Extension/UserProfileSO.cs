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
    public event Action OnFriendListChanged; // Sự kiện B

    public void UpdateFriendList()
    {
        OnFriendListChanged?.Invoke(); // Chỉ gọi ai quan tâm đến Friend
    }
}