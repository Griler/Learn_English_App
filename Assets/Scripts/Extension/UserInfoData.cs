using UnityEngine;

[CreateAssetMenu(fileName = "UserInfoData", menuName = "Game Data/User Info Data")]
public class UserInfoData : ScriptableObject
{
    [Header("User Info")]
    public string userName;
    public string email;
    public int coin;

    [Header("Login & Progress")]
    public bool isLoggedIn;
    public string userId; // Firebase UID nếu bạn có

    [Header("Daily Mission")]
    public string lastLoginDate;
}