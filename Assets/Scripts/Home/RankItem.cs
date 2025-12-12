using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankItem : BaseCode
{
    public  TextMeshProUGUI rankText;
    public  TextMeshProUGUI name;
    public  Image avatar;
    public  Image border;
    public int indexRank;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void setData(UserInfoData userInfo)
    {
        string username = userInfo.name;
        name.text = username;
        avatar.sprite = assetManager.getSpriteAvatar(userInfo.avatar);
        border.sprite = assetManager.getSpriteBorder(userInfo.border);
        rankText.text = "Rank Point: " + userInfo.rankPoint.ToString();
        gameObject.SetActive(true);
    }
}
