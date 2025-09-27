using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using TMPro;

namespace demo
{

    public class FirebaseAuthTest : MonoBehaviour
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
    }
}