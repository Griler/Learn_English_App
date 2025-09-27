using UnityEngine;
using Facebook.Unity;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.UI;

public class FacebookLoginManager : MonoBehaviour
{
    private FirebaseAuth auth;
    public Button loginButton;
    void Awake()
    {
        loginButton.onClick.AddListener(LoginWithFacebook);
      
    }

    // Gọi khi user bấm nút "Login with Facebook"
    public void LoginWithFacebook()
    {
        if (!FB.IsInitialized)
            FB.Init(() => FB.ActivateApp());
        else
            FB.ActivateApp();

        auth = FirebaseAuth.DefaultInstance;
        
        var perms = new System.Collections.Generic.List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perms, AuthCallback);
    }

    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            // Lấy access token từ Facebook
            var aToken = AccessToken.CurrentAccessToken;
            Debug.Log("Facebook Access Token: " + aToken.TokenString);

            // Đưa token vào Firebase
            Credential credential = FacebookAuthProvider.GetCredential(aToken.TokenString);
            SignInWithFirebase(credential);
        }
        else
        {
            Debug.Log("Facebook login failed: " + result.Error);
        }
    }

    private void SignInWithFirebase(Credential credential)
    {
        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Firebase login failed: " + task.Exception);
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.LogFormat("Firebase user logged in: {0} ({1})", newUser.DisplayName, newUser.UserId);
        });
    }
}