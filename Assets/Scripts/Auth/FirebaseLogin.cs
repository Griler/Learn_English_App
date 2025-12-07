using System;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement; // Nếu bạn dùng TextMeshPro cho UI

public class FirebaseLogin : MonoBehaviour
{
    [Header("Firebase")] private FirebaseAuth auth;

    private FirebaseUser user;

    // Tham chiếu đến Database
    [Header("UI References Login")] public GameObject loginForm;
    public TMP_InputField emailInputLogin;
    public TMP_InputField passwordInputLogin;
    public TMP_Text statusTextLoginForm;

    [Header("UI References Register")] public GameObject registerForm;
    public TMP_InputField emailInputResigter;
    public TMP_InputField passwordInputResigter;
    public TMP_InputField confirmPasswordInput;
    public TMP_Text statusTextResigterForm;

    [Header("UI References Popup")] public NotificationManager popupNotification;

    private void Start()
    {
        setActiveLoginForm(true);
        setActiveRegisterForm(false);
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
    }

    public void OnLoginButtonPressed()
    {
        string email = emailInputLogin.text;
        string password = passwordInputLogin.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusTextLoginForm.text = "Vui lòng nhập đầy đủ email và mật khẩu.";
            statusTextResigterForm.color = new Color32(220, 20, 60, 255);
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
                default:
                    message += "Lỗi không xác định.";
                    break;
            }

            statusTextLoginForm.text = message;
            statusTextResigterForm.color = new Color32(220, 20, 60, 255);
        }
        else
        {
            user = loginTask.Result.User;
            popupNotification.ShowNotification(" Đăng nhập thành công!");
            //statusTextLoginForm.text = $" Đăng nhập thành công! Xin chào {user.Email}";
            loadNextScene();
        }
    }

    public void OnRegisterButtonPressed()
    {
        string email = emailInputResigter.text.Trim();
        string password = passwordInputResigter.text;
        string confrimPassword = confirmPasswordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusTextResigterForm.text = " Vui lòng nhập email và mật khẩu để đăng ký.";
            statusTextResigterForm.color = new Color32(220, 20, 60, 255);
            return;
        }

        if (password != confrimPassword)
        {
            statusTextResigterForm.text = " Vui lòng nhập xác nhập mật khẩu giống nhau để đăng ký.";
            statusTextResigterForm.color = new Color32(220, 20, 60, 255);
            return;
        }

        StartCoroutine(RegisterUser(email, password));
    }

    private IEnumerator RegisterUser(string email, string password)
    {
        // BƯỚC 1: Tạo tài khoản Authentication
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            statusTextResigterForm.text = "Lỗi đăng ký: " + registerTask.Exception.GetBaseException().Message;
            yield break; // Dừng coroutine nếu lỗi
        }

        // Đăng ký Auth thành công
        FirebaseUser newUser = registerTask.Result.User;
        string name = email.Split("@")[0];
        UserInfoData userData = new UserInfoData("ava_1", "border_0", email, name, 0, 0);

        string json = JsonUtility.ToJson(userData);

        // BƯỚC 3: Lưu vào Realtime Database theo UserId
        // Đường dẫn: users -> [UserID] -> {data}
        var dbTask = FirebaseDatabaseManager.Instance.dbReference
            .Child("users")
            .Child(newUser.UserId)
            .Child("userInfo")
            .SetRawJsonValueAsync(json);

        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            statusTextResigterForm.text = "Đăng ký Auth xong nhưng lỗi lưu Database!";
            Debug.LogError(dbTask.Exception.ToString());
        }
        else
        {
            user = newUser;
            popupNotification.ShowNotification($"Đăng ký thành công! Email: {user.Email}");

            // Reset form hoặc chuyển scene tùy bạn
            onMovetoLoginForm();
        }
    }

    public void onMovetoRegisterForm()
    {
        setActiveLoginForm(false);
        setActiveRegisterForm(true);
        resetDataInput();
    }

    public void onMovetoLoginForm()
    {
        setActiveLoginForm(true);
        setActiveRegisterForm(false);
        resetDataInput();
    }

    private void setActiveLoginForm(bool enable = true)
    {
        loginForm.SetActive(enable);
    }

    private void setActiveRegisterForm(bool enable = true)
    {
        registerForm.SetActive(enable);
    }

    private void resetDataInput()
    {
        emailInputResigter.text = "";
        emailInputLogin.text = "";
        passwordInputLogin.text = "";
        passwordInputResigter.text = "";
        confirmPasswordInput.text = "";
    }

    private void loadNextScene()
    {
        // Ví dụ: load scene có tên "GameScene"
        SceneManager.LoadSceneAsync("HomeScene");
    }
}