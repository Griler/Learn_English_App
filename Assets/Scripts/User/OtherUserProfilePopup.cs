using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OtherUserProfilePopup : MonoBehaviour
{
    public static OtherUserProfilePopup Instance;

    [Header("UI Refs")]
    public GameObject popupPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI coinText;
    // public Image avatarImage;

    private void Awake() { Instance = this; }

    public void ShowPopup(UserInfoData info)
    {
        nameText.text = info.name;
        emailText.text = "Email: " + info.email;
        coinText.text = "Coin: " + info.coin;
        
        popupPanel.SetActive(true);
    }

    // Gắn vào nút X (Close) trên popup
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}