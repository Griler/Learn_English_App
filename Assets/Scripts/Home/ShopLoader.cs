using System;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopLoader : MonoBehaviour
{
    private DatabaseReference dbRef;

    private void OnEnable()
    {
        LoadShopData();
    }

    public void LoadShopData()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("shop")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Load shop failed: " + task.Exception);
                    return;
                }

                if (task.IsCompleted)
                {
                    string json = task.Result.GetRawJsonValue();

                    ShopData shop = JsonConvert.DeserializeObject<ShopData>(json);

                    Debug.Log("=== BORDERS ===");
                    foreach (var b in shop.Borders)
                        Debug.Log($"{b.Id} - {b.Name}");

                    Debug.Log("=== AVATARS ===");
                    foreach (var a in shop.Avatars)
                        Debug.Log($"{a.Id} - {a.Name}");
                }
            });
    }}