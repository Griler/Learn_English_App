using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random; // Nếu bạn dùng TextMeshPro cho UI

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
            popupNotification.ShowNotification(" Đăng nhập thành công!");
            //statusTextLoginForm.text = $" Đăng nhập thành công! Xin chào {user.Email}";
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
                    break; ;
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
            if (user.IsEmailVerified) {
                Debug.Log("Login thành công & Đã verify.");
            } else {
                Debug.Log("Login đúng pass nhưng chưa verify email.");
            }
            popupNotification.ShowNotification(" Đăng nhập thành công!");
            FirebaseDatabaseManager.Instance.currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
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
        
        int minLengthPw = 6;
        if (password.Length < 6 || confrimPassword.Length < 6 )
        {
            statusTextResigterForm.text = " Vui lòng nhập xác nhập mật khẩu trên 5 ký tự";
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
        string name = GetSimpleName(email);
        UserInfoData userData = new UserInfoData("avatar_1", "border_0", email, name, 0, 0);

        string json = JsonUtility.ToJson(userData);
        var task1 = FirebaseDatabaseManager.Instance.dbReference.Child("users").Child(newUser.UserId)
            .Child("items").Child("avatars").Child("avatar_1")
            .SetValueAsync(true);
        var task2 = FirebaseDatabaseManager.Instance.dbReference
            .Child("users")
            .Child(newUser.UserId)
            .Child("userInfo")
            .SetRawJsonValueAsync(json);

        var allTasks = Task.WhenAll(task1, task2);
        yield return new WaitUntil(() => allTasks.IsCompleted);

        if (allTasks.Exception != null)
        {
            statusTextResigterForm.text = "Lỗi lưu Database!";
            Debug.LogError(allTasks.Exception.ToString());
        }
        else
        {
            user = newUser;
            Debug.Log("Bắt đầu gửi email xác thực...");
            var sendEmailTask = user.SendEmailVerificationAsync();
            yield return new WaitUntil(() => sendEmailTask.IsCompleted);
            if (sendEmailTask.Exception != null)
            {
                Debug.LogError("Gửi mail thất bại: " + sendEmailTask.Exception.GetBaseException().Message);
                ToastSystem.Instance.ShowToast("Lỗi gửi mail: " + sendEmailTask.Exception.GetBaseException().Message);
            }
            else
            {
                Debug.Log("Đã gửi email thành công!");
                ToastSystem.Instance.ShowToast("Đăng ký thành công! Vui lòng check mail.");
                onMovetoLoginForm();
            }
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
    
    public string GetSimpleName(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@")) 
            return "User_" + Random.Range(1000, 9999);

        // Cắt lấy phần trước chữ @
        string localPart = email.Split('@')[0];

        // Thêm đuôi số random
        int randomSuffix = Random.Range(100, 9999);

        return $"{localPart}_{randomSuffix}";
    }

    public void SendVerificationEmail()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user != null)
        {
            user.SendEmailVerificationAsync().ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("Gửi email bị hủy.");
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError("Lỗi gửi email: " + task.Exception);
                    return;
                }

                Debug.Log("Đã gửi email xác thực thành công!");
                // Hiển thị UI thông báo: "Đã gửi mail, vui lòng kiểm tra hòm thư"
            });
        }
    }
}