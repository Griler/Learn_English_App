using System;
using System.Collections.Generic;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneManager : MonoBehaviour
{
    private void Start()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        Debug.Log(currentUser.Email);
    }
}
