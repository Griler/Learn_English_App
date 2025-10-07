using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using TMPro; // Nếu bạn dùng TextMeshPro cho UI

namespace demo
{
    /*public class FirebaseAuthTest : MonoBehaviour
    {
        // 💡 KHAI BÁO BIẾN UI
        [SerializeField] private TextMeshProUGUI statusText;
        // ^ Thay thế bằng Text nếu bạn không dùng TextMeshPro

        private FirebaseAuth auth;
        private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;

        void Start()
        {
            // Kiểm tra xem đã gán Text UI chưa
            if (statusText == null)
            {
                Debug.LogError("🔴 LỖI: Chưa gán Text UI (TextMeshProUGUI) vào biến statusText trong Inspector!");
                return;
            }

            UpdateStatus("⏳ Đang kiểm tra cấu hình Firebase...");

            // Bắt đầu kiểm tra phụ thuộc
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                dependencyStatus = task.Result;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebaseAuth();
                }
                else
                {
                    UpdateStatus($"🔴 LỖI CẤU HÌNH: Không thể giải quyết các phụ thuộc Firebase: {dependencyStatus}");
                }
            });
        }

        void InitializeFirebaseAuth()
        {
            UpdateStatus("🟢 Đã giải quyết phụ thuộc! Đang khởi tạo Firebase Auth...");

            auth = FirebaseAuth.DefaultInstance;

            if (auth != null)
            {
                UpdateStatus("🎉 THÀNH CÔNG: Firebase Authentication đã được khởi tạo.");
                CheckAuthState();
            }
            else
            {
                UpdateStatus("🔴 LỖI KHỞI TẠO: FirebaseAuth.DefaultInstance là NULL.");
            }
        }

        void CheckAuthState()
        {
            FirebaseUser user = auth.CurrentUser;
            if (user != null)
            {
                string displayName = string.IsNullOrEmpty(user.DisplayName) ? "Ẩn danh" : user.DisplayName;
                string email = string.IsNullOrEmpty(user.Email) ? "Không có Email" : user.Email;

                UpdateStatus(
                    $"✅ Đã kết nối và tìm thấy người dùng hiện tại:\nUID: {user.UserId}\nEmail: {email}\nTên: {displayName}");
            }
            else
            {
                // Sẵn sàng cho quá trình đăng nhập. Bắt đầu test kết nối server.
                UpdateStatus("✅ KẾT NỐI SẴN SÀNG: Không có người dùng nào đang đăng nhập.");
                TestAnonymousSignIn();
            }
        }

        void TestAnonymousSignIn()
        {
            UpdateStatus(statusText.text + "\n⏳ Đang thử kết nối máy chủ Auth (Ẩn danh)...");

            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    // Xử lý lỗi như đã sửa ở trên
                    UpdateStatus($"🔴 LỖI KẾT NỐI SERVER: Đăng nhập ẩn danh thất bại.");
                    return;
                }

                // Đã sửa lỗi AuthResult/FirebaseUser ở đây
                AuthResult result = task.Result;
                FirebaseUser newUser = result.User;

                UpdateStatus(statusText.text + $"\n✨ THÀNH CÔNG KẾT NỐI SERVER!\nUID test: {newUser.UserId}");

                // Đăng xuất ngay lập tức
                auth.SignOut();
                UpdateStatus(statusText.text + "\nĐã đăng xuất người dùng ẩn danh.");
            });
        }

        // 💡 HÀM HỖ TRỢ: Cập nhật Text UI
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log(message); // Vẫn giữ Debug.Log để xem trong Console
        }
    }*/

    public class FirebaseLogin : MonoBehaviour
    {
        [Header("Firebase")] private FirebaseAuth auth;
        private FirebaseUser user;

        [Header("UI References")] public TMP_InputField emailInput;
        public TMP_InputField passwordInput;
        public TMP_Text statusText;

        void Start()
        {
            InitializeFirebase();
        }

        void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    statusText.text = "Firebase ready ✅";
                }
                else
                {
                    statusText.text = $"Firebase error: {dependencyStatus}";
                }
            });
        }

        public void OnLoginButtonPressed()
        {
            string email = "thienloc662001@gmail.com";
            string password = "123456";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                statusText.text = "⚠️ Vui lòng nhập đầy đủ email và mật khẩu.";
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

                statusText.text = message;
            }
            else
            {
                user = loginTask.Result.User;
                statusText.text = $"🎉 Đăng nhập thành công! Xin chào {user.Email}";
            }
        }

        public void OnRegisterButtonPressed()
        {
            string email = emailInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                statusText.text = "⚠️ Vui lòng nhập email và mật khẩu để đăng ký.";
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
                statusText.text = "❌ Lỗi khi đăng ký tài khoản.";
            }
            else
            {
                user = registerTask.Result.User;
                statusText.text = $"✅ Tạo tài khoản thành công: {user.Email}";
            }
        }
    }
}