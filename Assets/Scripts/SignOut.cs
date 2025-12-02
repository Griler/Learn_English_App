using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SignOut : MonoBehaviour
{
    public Button singOut;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                singOut.onClick.AddListener((() =>
                {
                    FirebaseAuth.DefaultInstance.SignOut();
                    SceneManager.LoadScene("LoginScene");
                }));
            }

        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
