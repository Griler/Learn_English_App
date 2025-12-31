using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RegisterManager : MonoBehaviour
{
    [Header("UI References Register")] public GameObject registerForm;
    public TMP_InputField emailInputResigter;
    public TMP_InputField passwordInputResigter;
    public TMP_InputField confirmPasswordInput;
    public GameObject statusTextResigterForm;
    public GameObject loginForm;
    public NoticeLogin noticeLogin;
    public Button linkEmailButton;
    private const int minLengthPw = 6;

    private void Start()
    {
        if (linkEmailButton)
        {
            linkEmailButton.onClick.AddListener(LinkEmailToGuest);
        }
    }

    public void OnRegisterButtonPressed()
    {
        string email = emailInputResigter.text.Trim();
        string password = passwordInputResigter.text;
        string confrimPassword = confirmPasswordInput.text;
        if (IsVailInput())
        {
            StartCoroutine(RegisterUser(email, password));
        }
    }

    bool IsVailInput()
    {
        string email = emailInputResigter.text.Trim();
        string password = passwordInputResigter.text;
        string confrimPassword = confirmPasswordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusTextResigterForm.SetActive(true);
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().text =
                "Vui lòng nhập email và mật khẩu để đăng ký.";
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.softRed;
            return false;
        }

        if (password.Length < minLengthPw || confrimPassword.Length < minLengthPw)
        {
            statusTextResigterForm.SetActive(true);
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().text =
                " Vui lòng nhập xác nhập mật khẩu trên 5 ký tự";
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.softRed;
            return false;
        }

        if (password != confrimPassword)
        {
            statusTextResigterForm.SetActive(true);
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().text =
                " Vui lòng nhập xác nhập mật khẩu giống nhau để đăng ký.";
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.softRed;
            return false;
        }

        return true;
    }

    private IEnumerator RegisterUser(string email, string password)
    {
        // --- BƯỚC 1: Đăng ký Authentication ---
        var registerTask =
            FirebaseDatabaseManager.Instance.fireAuthReference.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            FirebaseException firebaseEx = (FirebaseException)registerTask.Exception.GetBaseException();
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Đăng nhập thất bại: ";

            switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    message += "Email không hợp lệ.";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message += "Email đã được dùng";
                    break;
            }

            statusTextResigterForm.SetActive(true);
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().text = message;
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.softRed;
            yield break;
        }

        FirebaseUser newUser = registerTask.Result.User;

        // --- BƯỚC 2: Tạo Random Username (Đã sửa đoạn này) ---
        // Gọi hàm async nhưng không dùng 'await', mà gán vào biến Task
        var usernameTask = FirebaseDatabaseManager.Instance.GetUniqueUsernameAsync();

        // Đợi cho đến khi hàm tìm tên chạy xong
        yield return new WaitUntil(() => usernameTask.IsCompleted);

        if (usernameTask.Exception != null)
        {
            Debug.LogError("Lỗi tạo username: " + usernameTask.Exception.ToString());
            // Có thể xử lý fallback hoặc return tại đây nếu cần
        }

        // Lấy kết quả từ Task
        string uniqueUserName = usernameTask.Result;
        string displayName = GetSimpleName(email); // Tên hiển thị từ email

        // --- BƯỚC 3: Lưu vào Database ---
        // LƯU Ý: Mình đã thay thế biến 'name' thứ 2 bằng 'uniqueUserName' vừa tạo được
        // Bạn hãy kiểm tra lại Constructor của UserInfoData xem thứ tự tham số đúng chưa nhé
        UserInfoData userData = new UserInfoData(
            "avatar_1",
            "border_0",
            email,
            displayName, // Tên hiển thị (VD: Tuấn)
            uniqueUserName, // Username duy nhất (VD: User_XyZ123) -> QUAN TRỌNG
            0,
            0
        );

        string json = JsonUtility.ToJson(userData);

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
            statusTextResigterForm.SetActive(true);
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().text = "Lỗi đăng ký vui lòng thủ lại";
            statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().color = Color.softRed;
            Debug.LogError(allTasks.Exception.ToString());
        }
        else
        {
            // --- BƯỚC 4: Gửi Email xác thực ---
            Debug.Log("Bắt đầu gửi email xác thực...");
            var sendEmailTask = newUser.SendEmailVerificationAsync();
            yield return new WaitUntil(() => sendEmailTask.IsCompleted);

            if (sendEmailTask.Exception != null)
            {
                Debug.LogError("Lỗi gửi mail: " + sendEmailTask.Exception.GetBaseException().Message);
            }
            else
            {
                Debug.Log("Đã gửi email thành công!");
                Action callback = () =>
                {
                    noticeLogin.container.SetActive(false);
                    onMovetoLoginForm();
                };
                noticeLogin.showNotice("Đăng ký thành công! \nVui lòng check mail.", callback);
                onMovetoLoginForm();
            }
        }
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

    public void onMovetoLoginForm()
    {
        registerForm.SetActive(false);
        loginForm.SetActive(true);
        resetDataInput();
    }

    private void resetDataInput()
    {
        emailInputResigter.text = "";
        passwordInputResigter.text = "";
        confirmPasswordInput.text = "";
        statusTextResigterForm.SetActive(false);
        statusTextResigterForm.GetComponentInChildren<TextMeshProUGUI>().text = "";
    }

    public void LinkEmailToGuest()
    {
        var user = FirebaseDatabaseManager.Instance.currentUser; // Đảm bảo biến này lấy từ FirebaseAuth
        if (user == null) return;

        // 1. Validate Input trước
        if (!IsVailInput()) return;

        statusTextResigterForm.SetActive(false);
        string newEmail = emailInputResigter.text.Trim(); // Nên Trim() để xóa khoảng trắng thừa
        string newPassword = passwordInputResigter.text;

        linkEmailButton.interactable = false;

        // Tạo credential
        Credential credential = EmailAuthProvider.GetCredential(newEmail, newPassword);

        // 2. Gọi hàm Link
        user.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            // Luôn bật lại nút dù thành công hay thất bại để tránh bị treo app
            linkEmailButton.interactable = true;

            if (task.IsCanceled)
            {
                Debug.LogError("Hủy liên kết.");
                return;
            }

            if (task.IsFaulted)
            {
                FirebaseException firebaseEx = (FirebaseException)task.Exception.GetBaseException();
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Liên kết thất bại: ";

                switch (errorCode)
                {
                    // SỬA 1: Dùng đúng mã lỗi cho hàm Link
                    case AuthError.CredentialAlreadyInUse :
                    case AuthError.EmailAlreadyInUse:
                        message = "Email này đã được tài khoản khác sử dụng!";
                        break;
                    case AuthError.ProviderAlreadyLinked:
                        message = "Tài khoản này đã liên kết Email rồi, không cần liên kết lại.";
                        break;
                    case AuthError.WeakPassword:
                        message = "Mật khẩu quá yếu (cần ít nhất 6 ký tự).";
                        break;

                    case AuthError.InvalidEmail:
                        message = "Định dạng email không đúng.";
                        break;
                    default:
                        message += firebaseEx.Message;
                        break;
                }

                Debug.LogError("Lỗi: " + errorCode);
                ToastSystem.Instance.ShowToast(message);
                return;
            }

            FirebaseUser newUser = task.Result.User;
            user.ReloadAsync();
            ToastSystem.Instance.ShowToast("Liên kết thành công!");
            Debug.LogFormat("Thành công! User {0} đã link với {1}", newUser.UserId, newEmail);
        });
    }
}