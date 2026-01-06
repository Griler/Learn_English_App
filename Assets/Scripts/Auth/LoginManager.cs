using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class LoginManager : MonoBehaviour
{
    [Header("Firebase")] private FirebaseAuth auth;
    [Header("UI References Login")] public TMP_InputField emailInputLogin;
    public TMP_InputField passwordInputLogin;
    public GameObject statusTextLoginForm;
    public GameObject registerForm;
    public GameObject mainForm;
    public GameObject loginForm;
    public NoticeLogin noticeLogin;

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
        FirebaseDatabaseManager.Instance.OnFirebaseInitialized -= setUserAuth;
    }

    void setUserAuth()
    {
        auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null) return;
        if (auth.CurrentUser.IsAnonymous)
        {
            loadNextScene();
            return;
        }
        if (auth.CurrentUser.IsEmailVerified)
        {
            loadNextScene();;
        }
        else
        {
            auth.SignOut();
        }
    }

    public void OnLoginButtonPressed()
    {
        string email = emailInputLogin.text;
        string password = passwordInputLogin.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusTextLoginForm.SetActive(true);
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text =
                "Vui lòng nhập đầy đủ email và mật khẩu.";
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().color = new Color32(220, 20, 60, 255);
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
                    message += "Vui lòng thử lại";
                    break;
            }

            statusTextLoginForm.SetActive(true);
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text = message;
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().color = new Color32(220, 20, 60, 255);
        }
        else
        {
            FirebaseUser user = loginTask.Result.User;
            if (user.IsEmailVerified)
            {
                noticeLogin.showNotice("Đăng nhập thành công vui lòng chờ ...");
                FirebaseDatabaseManager.Instance.currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
                loadNextScene();
            }
            else
            {
                Debug.Log("Login đúng pass nhưng chưa verify email.");
                Action callback = () => { noticeLogin.container.SetActive(false); };
                noticeLogin.showNotice("Vui lòng check mail. \nXác thực người dùng", callback);
                noticeLogin.sendMailButton.gameObject.SetActive(true);
                noticeLogin.sendMailButton.onClick.AddListener(() =>
                {
                    StartCoroutine(sendMailButton(user));
                    noticeLogin.sendMailButton.onClick.RemoveAllListeners();
                    noticeLogin.sendMailButton.gameObject.SetActive(false);
                });
            }
        }
    }

    IEnumerator sendMailButton(FirebaseUser newUser)
    {
        var sendEmailTask = newUser.SendEmailVerificationAsync();
        yield return new WaitUntil(() => sendEmailTask.IsCompleted);

        if (sendEmailTask.Exception != null)
        {
            Debug.LogError("Lỗi gửi mail: " + sendEmailTask.Exception.GetBaseException().Message);
        }
        else
        {
            Debug.Log("Đã gửi email thành công!");
            statusTextLoginForm.SetActive(true);
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text = "Gửi thành công! \nVui lòng check mail.";
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.green;
            noticeLogin.container.SetActive(false);
        }
    }

    public void loadNextScene()
    {
        // Ví dụ: load scene có tên "GameScene"
        SceneManager.LoadSceneAsync("HomeScene");
    }

    // Hàm này gắn vào nút bấm Button
    public void SignInAnonymously()
    {
        Action callback = () =>
        {
            StartCoroutine(SignInAnonymouslyCoroutine());
        };
        noticeLogin.showNotice("Tài khoản khách sẽ mất khi bạn đổi thiết bị hoặc xoá dữ liệu.\n" +
                               " Bạn có thể liên kết email để không mất dữ liệu", callback);
    }

    private IEnumerator SignInAnonymouslyCoroutine()
    {
        // --- BƯỚC 1: Đăng nhập Auth ---
        Debug.Log("Đang đăng nhập chế độ khách...");
        var authTask = auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => authTask.IsCompleted);

        if (authTask.Exception != null)
        {
            Debug.LogError("Lỗi đăng nhập khách: " + authTask.Exception.GetBaseException().Message);
            yield break;
        }

        FirebaseUser newUser = authTask.Result.User;
        Debug.LogFormat("Auth thành công: {0}", newUser.UserId);

        // --- BƯỚC 2: Tạo Username ngẫu nhiên ---
        // Khách cũng cần 1 cái tên định danh (VD: User_XyZ)
        var usernameTask = FirebaseDatabaseManager.Instance.GetUniqueUsernameAsync();
        yield return new WaitUntil(() => usernameTask.IsCompleted);

        if (usernameTask.Exception != null)
        {
            Debug.LogError("Lỗi tạo tên: " + usernameTask.Exception.ToString());
            yield break;
        }

        string uniqueUserName = usernameTask.Result;

        // --- BƯỚC 3: Chuẩn bị dữ liệu ---
        // Với khách: Email rỗng, Tên hiển thị đặt tạm là "Guest" hoặc lấy luôn ID
        UserInfoData userData = new UserInfoData(
            "avatar_1", // Avatar mặc định
            "border_0", // Border mặc định
            "", // Email: Không có
            "Khách", // DisplayName: Để là Khách
            uniqueUserName, // Username: Cái tên User_xxx vừa tạo -> QUAN TRỌNG
            0, // Coin
            0 // RankPoint
        );

        string json = JsonUtility.ToJson(userData);

        // --- BƯỚC 4: Lưu vào Database ---
        var task1 = FirebaseDatabaseManager.Instance.dbReference
            .Child("users").Child(newUser.UserId)
            .Child("items").Child("avatars").Child("avatar_1")
            .SetValueAsync(true);

        var task2 = FirebaseDatabaseManager.Instance.dbReference
            .Child("users").Child(newUser.UserId)
            .Child("userInfo")
            .SetRawJsonValueAsync(json);

        var allTasks = Task.WhenAll(task1, task2);
        yield return new WaitUntil(() => allTasks.IsCompleted);

        if (allTasks.Exception != null)
        {
            Debug.LogError("Lỗi lưu DB: " + allTasks.Exception.ToString());
        }
        else
        {
            Debug.Log("Đăng nhập khách và tạo dữ liệu thành công!");
            loadNextScene();
        }
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
        statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text = "";
        statusTextLoginForm.SetActive(false);
    }

    public void onClickForgotPassword()
    {
        string email = emailInputLogin.text;
        
        if (string.IsNullOrEmpty(email))
        {
            statusTextLoginForm.SetActive(true);
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text =
                "Vui lòng nhập email bạn muốn đổi mật khẩu";
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().color = new Color32(220, 20, 60, 255);
            return;
        }
        StartCoroutine(SendResetPasswordEmail(email));
    }
    
    private IEnumerator SendResetPasswordEmail(string email)
    {
        Debug.Log($"Đang gửi yêu cầu reset password tới: {email}");

        // 2. Gọi hàm của Firebase
        var task = auth.SendPasswordResetEmailAsync(email);

        // 3. Đợi tác vụ hoàn thành
        yield return new WaitUntil(() => task.IsCompleted);

        // 4. Kiểm tra kết quả
        if (task.Exception != null)
        {
            // Lấy lỗi gốc
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            if (firebaseEx != null)
            {
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                string message = "";
                switch (errorCode)
                {
                    case AuthError.InvalidEmail:
                        message = "Email sai định dạng.";
                        break;
                    case AuthError.UserNotFound:
                        message = "Email này chưa từng đăng ký tài khoản!";
                        break;
                    case AuthError.NetworkRequestFailed:
                        message = "Lỗi mạng. Vui lòng kiểm tra Wifi/4G.";
                        break;
                    default:
                        message = "Lỗi: " + "Không tìm thấy tài khoản";
                        break;
                }
                statusTextLoginForm.SetActive(true);
                statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text = message; 
                statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().color = new Color32(220, 20, 60, 255);
            }
        }
        else
        {
            // THÀNH CÔNG
            Debug.Log("Gửi mail reset thành công!");
            string msg = ""; // Xóa thông báo lỗi cũ
            statusTextLoginForm.SetActive(true);
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().text = "Gửi mail reset thành công!"; 
            statusTextLoginForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.green;
            Action callback = () =>
            {
                noticeLogin.container.SetActive(false);
            };
            noticeLogin.showNotice("Đã gửi link đổi mật khẩu! \nHãy kiểm tra Email.", callback);
        }
    }
    private bool IsEmailValid(string email)
    {
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }
}