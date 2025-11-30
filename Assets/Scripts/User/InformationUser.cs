using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InformationUser : MonoBehaviour
{
    public Image avatar;
    public Image avatarBorder;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI coinText;

    public UserProfileSO userProfileSO;
    

    private void OnEnable()
    {
        userProfileSO.OnUserInfoChanged += updateInformation;
    }

    private void Awake()
    {
        FirebaseDatabaseManager.Instance.ListenToUserInfo();
    }

    public void updateInformation(UserInfoData userInfo)
    {
        string username = userInfo.name;
        int coin = userInfo.coin;
        usernameText.text = username;
        coinText.text = coin.ToString();
    }

    private void OnDisable()
    {
        userProfileSO.OnUserInfoChanged -= updateInformation;
    }
}
