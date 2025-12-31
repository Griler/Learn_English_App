using System.Collections;
using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;

public class RegisterManager : MonoBehaviour
{
    [Header("UI References Register")] public GameObject registerForm;
    public TMP_InputField emailInputResigter;
    public TMP_InputField passwordInputResigter;
    public TMP_InputField confirmPasswordInput;
    public TMP_Text statusTextResigterForm;
    public GameObject loginForm;
    private const int minLengthPw = 6;

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
        
        if (password.Length < minLengthPw || confrimPassword.Length < minLengthPw )
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
        var registerTask = FirebaseDatabaseManager.Instance.fireAuthReference.CreateUserWithEmailAndPasswordAsync(email, password);
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
            FirebaseUser user = newUser;
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
        //(true);
        gameObject.SetActive(false);
        resetDataInput();
    }
    
    private void resetDataInput()
    {
        emailInputResigter.text = "";
        passwordInputResigter.text = "";
        confirmPasswordInput.text = "";
    }
}
