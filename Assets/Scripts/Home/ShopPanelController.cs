using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    public Button avatarShopButton;
    public Button borderShopButton;
    public Button settingButton;

    public GameObject shopAvatar;
    public GameObject shopBorder;
    public GameObject settingView;
    public Button overlay;

    public ShopLoader ShopLoader;
    public TextMeshProUGUI nameTitle;
    void Start()
    {
        if (avatarShopButton)
            avatarShopButton.onClick.AddListener(openshopAvatar);
        if (borderShopButton)
            borderShopButton.onClick.AddListener(openshopBorder);
        if (settingButton)
            settingButton.onClick.AddListener(opensettingView);  
        if (overlay)
            overlay.onClick.AddListener((closePane));
    }

    private void OnEnable()
    {
        openshopAvatar();
    }

    void closePane()
    {
        gameObject.SetActive(false);
    }

    void openshopAvatar()
    {
        shopAvatar.SetActive(true);
        shopBorder.SetActive(false);
        settingView.SetActive(false);
        ShopLoader.LoadShopAvatars();
        nameTitle.text = "Cửa Hàng".ToUpper();
    }

    void openshopBorder()
    {
        shopAvatar.SetActive(false);
        shopBorder.SetActive(true);
        settingView.SetActive(false);
        ShopLoader.LoadShopBorders();
        nameTitle.text = "Shop".ToUpper();

    }

    void opensettingView()
    {
        shopAvatar.SetActive(false);
        shopBorder.SetActive(false);
        settingView.SetActive(true);
        nameTitle.text = "Cài Đặt".ToUpper();
    }

}
