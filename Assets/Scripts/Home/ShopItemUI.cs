using System;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : BaseCode
{
    [Header("UI Elements")]
    public Image iconImage;        // ảnh item
    public TMP_Text nameText;      // tên item
    public TMP_Text priceText;     // giá tiền

    public Button buyButton;       // nút mua
    public Button useButton;       // nút xài

    public GameObject lockOverlay; // tấm phủ mờ nếu chưa mua

    [Header("Item Data")]
    public string itemId;
    public string price;
    public bool owned = false;     // đã mua?
    private DatabaseReference db;
    private string typeItemPatch = "";
    private string typeItemInfo = "";

    public void Start()
    {
        buyButton.onClick.AddListener(OnBuyClicked);;
        useButton.onClick.AddListener(OnUseClicked);;
    }

    public void SetupItem(string itemName, string itemPrice, string id, bool isOwned, string typeItem)
    {
        typeItemInfo = typeItem;
        if (typeItem == "avatar")
        {
            iconImage.sprite = assetManager.getSpriteAvatar(id);
            typeItemPatch = "avatars";
        }
        else if (typeItem == "border")
        {
            iconImage.sprite = assetManager.getSpriteBorder(id);
            typeItemPatch = "borders";

        } 
        nameText.text = itemName;
        priceText.text = itemPrice;

        itemId = id;
        price = itemPrice;
        owned = isOwned;

        UpdateUI();
    }

    // Cập nhật trạng thái UI
    void UpdateUI()
    {
        if (owned)
        {
            lockOverlay.SetActive(false);
            buyButton.gameObject.SetActive(false);
            useButton.gameObject.SetActive(true);
            useButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                FirebaseDatabaseManager.Instance.userProfileSO.userInfo.avatar == itemId ? "Đang dùng" : "Dùng";
        }
        else
        {
            lockOverlay.SetActive(true);
            buyButton.gameObject.SetActive(true);
            useButton.gameObject.SetActive(false);
            buyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Mua";
        }
    }

    // Khi nhấn nút mua
    public void OnBuyClicked()
    {
        Debug.Log("Mua item: " + itemId);
        db = FirebaseDatabaseManager.Instance.dbReference;
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        int currentCoin = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.coin;
        // 1) Lấy coin user
        db.Child("users").Child(userId).Child("userInfo").Child("coin").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted) return;
                
                if (currentCoin < Convert.ToInt32(price))
                {
                    Debug.Log("Không đủ tiền!");
                    ToastSystem.Instance.ShowToast("Không đủ tiền!");
                    return;
                }

                // 2) Trừ tiền
                int newCoin = currentCoin - Convert.ToInt32(price);

                db.Child("users").Child(userId).Child("userInfo").Child("coin").SetValueAsync(newCoin);

                // 3) Lưu item đã mua
                db.Child("users").Child(userId)
                    .Child("items").Child(typeItemPatch).Child(itemId)
                    .SetValueAsync(true);

                // 4) Update UI
                owned = true;
                UpdateUI();

                Debug.Log("Mua thành công: " + itemId);
            });
    }

    // ============================
    // ✔ DÙNG ITEM
    // ============================
    public void OnUseClicked()
    {
        Debug.Log("Dùng item: " + itemId);
        db = FirebaseDatabaseManager.Instance.dbReference;
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        // Lưu item đang xài
        db.Child("users").Child(userId)
           .Child("userInfo").Child(typeItemInfo)
            .SetValueAsync(itemId);

        Debug.Log("Đang dùng avatar: " + itemId);
    }
}