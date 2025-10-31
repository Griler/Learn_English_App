using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InformationUser : MonoBehaviour
{
    public Image avatar;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI coinText;


    public void updateInformation()
    {
        string email = PlayerPrefs.GetString("user");
        string username = PlayerPrefs.GetString("email");
        int coin = PlayerPrefs.GetInt("coin");
        usernameText.text = username;
        coinText.text = coin.ToString();
    }
}
