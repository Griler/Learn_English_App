using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using TMPro;
using UnityEngine.SceneManagement; // Nếu bạn dùng TextMeshPro cho UI

namespace demo
{
    public class FirebaseLogin : MonoBehaviour
    {
        [Header("Firebase")] private FirebaseAuth auth;
        private FirebaseUser user;

        [Header("UI References Login")] 
        public GameObject loginForm;
        public TMP_InputField emailInputLogin;
        public TMP_InputField passwordInputLogin;
        public TMP_Text statusTextLoginForm;
        
        [Header("UI References Register")] 
        public GameObject registerForm;
        public TMP_InputField emailInputResigter;
        public TMP_InputField passwordInputResigter;
        public TMP_InputField confirmPasswordInput;
        public TMP_Text statusTextResigterForm;
        
        void Start()
        {
            InitializeFirebase();
            setActiveLoginForm(true);
            setActiveRegisterForm(false);
        }

        void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    statusTextLoginForm.text = "Firebase ready ✅";
                }
                else
                {
                    statusTextLoginForm.text = $"Firebase error: {dependencyStatus}";
                }
            });
        }

        public void OnLoginButtonPressed()
        {
            string email = "thienloc662001@gmail.com";
            string password = "123456";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                statusTextLoginForm.text = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return;
            }

            StartCoroutine(LoginUser(email, password));
        }

        private IEnumerator LoginUser(string email, string password)
        {
            var loginTask = auth.SignInWithEmailAndPasswordAsync("thienloc662001@gmail.com", "123456");

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
            }
            else
            {
                user = loginTask.Result.User;
                statusTextLoginForm.text = $" Đăng nhập thành công! Xin chào {user.Email}";
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
                return;
            }

            if (password != confrimPassword)
            {
                statusTextResigterForm.text = " Vui lòng nhập xác nhập mật khẩu giống nhau để đăng ký.";
                return;
            }

            StartCoroutine(RegisterUser(email, password));
        }

        private IEnumerator RegisterUser(string email, string password)
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                statusTextResigterForm.text = "Lỗi khi đăng ký tài khoản.";
            }
            else
            {
                user = registerTask.Result.User;
                statusTextResigterForm.text = $" Tạo tài khoản thành công: {user.Email}";
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

        public void setActiveLoginForm(bool enable = true)
        {
            loginForm.SetActive(enable);
        }
        
        public void setActiveRegisterForm(bool enable = true)
        {
            registerForm.SetActive(enable);
        }

        void resetDataInput()
        {
            emailInputResigter.text = "";
            emailInputLogin.text = "";
            passwordInputLogin.text = "";
            passwordInputResigter.text = "";
            confirmPasswordInput.text = "";
        }
        
        public void loadNextScene()
        {
            // Ví dụ: load scene có tên "GameScene"
            SceneManager.LoadSceneAsync(GlobalSelection.mainScene);
        }
    }
}