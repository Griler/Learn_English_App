using System;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UI;

public class ShopLoader : MonoBehaviour
{
    public GameObject shopItemPrefab;
    public Transform contanierAvatar;
    public Transform contanierBorder;

    private DatabaseReference dbRef;
    private string userId;

    private List<ShopItem> shopAvatars;
    private List<ShopItem> shopBorders;

    private Dictionary<string, bool> userAvatars;
    private Dictionary<string, bool> userBorders;

    public GameObject panelConfirm;
    public Button buttonConfirm;
    public Button buttonCancel;

    
    public void LoadShopAvatars()
    {
        dbRef = FirebaseDatabaseManager.Instance.dbReference;
        userId = FirebaseDatabaseManager.Instance.currentUser.UserId;

        dbRef.Child("shop").Child("avatars").GetValueAsync()
            .ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                ToastNetwork.Instance.actionOnClickButton = () => LoadShopAvatars();
                ToastNetwork.Instance.showDisconnect();
                return;
            }
            string json = task.Result.GetRawJsonValue();
            shopAvatars = JsonConvert.DeserializeObject<List<ShopItem>>(json);

            Debug.Log("Đã load shop avatars");

            LoadUserAvatars();
        });
    }

    // ===============================
    //      LOAD USER AVATARS
    // ===============================
    void LoadUserAvatars()
    {
        dbRef = FirebaseDatabaseManager.Instance.dbReference;
        userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        dbRef.Child("users").Child(userId).Child("items").Child("avatars")
          .GetValueAsync().ContinueWithOnMainThread(task =>
        {   
            if (task.IsCanceled || task.IsFaulted)
            {
                ToastNetwork.Instance.actionOnClickButton = () => LoadShopAvatars();
                ToastNetwork.Instance.showDisconnect();
                return;
            }
            userAvatars = new Dictionary<string, bool>();

            foreach (var child in task.Result.Children)
                userAvatars[child.Key] = true;

            Debug.Log("Đã load user avatars");

            CheckAvatarShop();
        });
    }

    // ===============================
    //      KIỂM TRA AVATAR SHOP
    // ===============================
    void CheckAvatarShop()
    {
        Debug.Log("=== KIỂM TRA AVATAR SHOP ===");
        foreach (Transform child in contanierAvatar)
        {
            Destroy(child.gameObject);
        }
        ToastNetwork.Instance.hideDisconnect();
        foreach (var item in shopAvatars)
        {
            if (userAvatars.ContainsKey(item.Id))
            {
                Debug.Log($"{item.Id} → Đã có");
                GameObject shopItem = Instantiate(shopItemPrefab, contanierAvatar);
                shopItem.GetComponent<ShopItemUI>().SetupItem(item.Name, item.Price, item.Id, true, "avatar", CheckAvatarShop);
            }
            else
            {
                Debug.Log($"{item.Id} → ko có");
                GameObject shopItem = Instantiate(shopItemPrefab, contanierAvatar);
                shopItem.GetComponent<ShopItemUI>().SetupItem(item.Name, item.Price, item.Id, false, "avatar", CheckAvatarShop);
            }
        }
    }

    // ===============================
    //      LOAD SHOP BORDERS
    // ===============================
    public void LoadShopBorders()
    {
        foreach (Transform c in contanierBorder) Destroy(c.gameObject);

        dbRef = FirebaseDatabaseManager.Instance.dbReference;
        userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        dbRef.Child("shop").Child("borders").GetValueAsync()
            .ContinueWithOnMainThread(task =>
        {
            string json = task.Result.GetRawJsonValue();
            shopBorders = JsonConvert.DeserializeObject<List<ShopItem>>(json);

            Debug.Log("Đã load shop borders");

            LoadUserBorders();
        });
    }

    // ===============================
    //      LOAD USER BORDERS
    // ===============================
    void LoadUserBorders()
    {
        dbRef = FirebaseDatabaseManager.Instance.dbReference;
        userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        dbRef.Child("users").Child(userId).Child("items").Child("borders")
          .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            userBorders = new Dictionary<string, bool>();

            foreach (var child in task.Result.Children)
                userBorders[child.Key] = true;

            Debug.Log("Đã load user borders");

            CheckBorderShop();
        });
    }

    // ===============================
    //      KIỂM TRA BORDER SHOP
    // ===============================
    void CheckBorderShop()
    {
       
    }
}