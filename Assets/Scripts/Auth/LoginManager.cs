using System.Collections;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [Header("Firebase")] private FirebaseAuth auth;
    [Header("UI References Login")] 
    public TMP_InputField emailInputLogin;
    public TMP_InputField passwordInputLogin;
    public TMP_Text statusTextLoginForm;
    public GameObject registerForm;
    public GameObject mainForm;
    public GameObject loginForm;

    private void Start()
    {
        loginForm.SetActive(false);
        mainForm.SetActive(true);
        registerForm.SetActive(false);
        
        if (FirebaseDatabaseManager.Instance.IsReady)
        {
            setUserAuth();
        }
    }

    private void OnEnable()
    {
        FirebaseDatabaseManager.Instance.OnFirebaseInitialized += setUserAuth;
    }

    private void OnDisable()
    {
        FirebaseDatabaseManager.Instance.OnFirebaseInitialized += setUserAuth;
    }

    void setUserAuth()
    {
        auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser != null)
        {
            //popupNotification.ShowNotification(" Đăng nhập thành công!");
            loadNextScene();
        }
    }

    public void OnLoginButtonPressed()
    {
        string email = emailInputLogin.text;
        string password = passwordInputLogin.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusTextLoginForm.text = "Vui lòng nhập đầy đủ email và mật khẩu.";
            statusTextLoginForm.color = new Color32(220, 20, 60, 255);
            return;
        }

        StartCoroutine(LoginUser(email, password));
    }

    private IEnumerator LoginUser(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            FirebaseException firebaseEx = (FirebaseException)loginTask.Exception.GetBaseException();
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Đăng nhập thất bại: ";

            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message += "Thiếu email.";
                    break;
                case AuthError.MissingPassword:
                    message += "Thiếu mật khẩu.";
                    break;
                case AuthError.WrongPassword:
                    message += "Sai mật khẩu.";
                    break;
                case AuthError.InvalidEmail:
                    message += "Email không hợp lệ.";
                    break;
                case AuthError.UserNotFound:
                    message += "Không tìm thấy người dùng.";
                    break;
                    ;
                default:
                    message += "Vui lòng thử lại.";
                    break;
            }

            statusTextLoginForm.text = message;
            statusTextLoginForm.color = new Color32(220, 20, 60, 255);
        }
        else
        {
            FirebaseUser user = loginTask.Result.User;
            if (user.IsEmailVerified)
            {
                Debug.Log("Login thành công & Đã verify.");
            }
            else
            {
                Debug.Log("Login đúng pass nhưng chưa verify email.");
            }

            FirebaseDatabaseManager.Instance.currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
            loadNextScene();
        }
    }

    private void loadNextScene()
    {
        // Ví dụ: load scene có tên "GameScene"
        SceneManager.LoadSceneAsync("HomeScene");
    }

    public void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("Đăng nhập khách bị hủy.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi đăng nhập khách: " + task.Exception);
                return;
            }
            FirebaseUser newUser = task.Result.User;
            Debug.LogFormat("Đăng nhập khách thành công: {0} ({1})",
                newUser.DisplayName, newUser.UserId);
        });
    }
    
    public void onMovetoRegisterForm()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(true);
        resetDataInput();
    }

    public void onMovetoLoginForm()
    {
        loginForm.SetActive(true);
        registerForm.SetActive(false);
        resetDataInput();
    }
    
    public void onMovetoMainForm()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(false);
        mainForm.SetActive(true);
        resetDataInput();
    }
    
    private void resetDataInput()
    {
        emailInputLogin.text = "";
        passwordInputLogin.text = "";
    }
}