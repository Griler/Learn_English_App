using System;
using UnityEngine;
using Firebase.Auth;
using ParrelSync; // Bắt buộc phải có package này
using System.Collections;
using Firebase;
using UnityEngine.SceneManagement;

public class AutoLoginDev : MonoBehaviour
{
    [Header("Tài khoản cho Editor Chính")]
    public bool autoLoginMain = false; // Tích vào nếu muốn Editor chính cũng tự login

    public string mainEmail = "1@gmail.com";
    public string mainPass = "123456";

    [Header("Tài khoản cho Clone (ParrelSync)")]
    public string cloneEmail = "thienloc662001@gmail.com";

    public string clonePass = "123456";

    public FriendPopupController friend;

    // Biến để đảm bảo không chạy login khi Firebase chưa sẵn sàng
    private bool isFirebaseReady = false;

    void Start()
    {
        // Đợi Firebase check dependency xong mới chạy logic (quan trọng!)
        StartCoroutine(WaitForFirebaseAndLogin());
    }

    IEnumerator WaitForFirebaseAndLogin()
    {
        // Chờ đến khi Firebase khởi tạo xong (Check biến static bên SystemInitializer của bạn)
        // Nếu bạn chưa có biến đó, có thể check thủ công:
        while (FirebaseAuth.DefaultInstance == null)
        {
            yield return null;
        }

        Debug.Log(">>> [AutoLogin] Firebase đã sẵn sàng. Bắt đầu kiểm tra môi trường...");

        if (ClonesManager.IsClone())
        {
            // === TRƯỜNG HỢP: ĐANG CHẠY TRÊN CLONE ===
            Debug.Log($"<color=yellow>[AutoLogin] Phát hiện CLONE (Arg: {ClonesManager.GetArgument()})</color>");

            // 1. Force Logout tài khoản cũ (để tránh dính từ Editor chính)
            if (FirebaseAuth.DefaultInstance.CurrentUser != null)
            {
                FirebaseAuth.DefaultInstance.SignOut();
                Debug.Log("[AutoLogin] Đã đăng xuất tài khoản cũ để tránh xung đột.");
            }

            // 2. Login tài khoản Clone
            PerformAutoLogin(cloneEmail, clonePass);
        }
        else
        {
            // === TRƯỜNG HỢP: EDITOR CHÍNH ===
            if (autoLoginMain)
            {
                Debug.Log("[AutoLogin] Đang chạy trên Editor Chính -> Auto Login Main User.");
                PerformAutoLogin(mainEmail, mainPass);
            }
            else
            {
                Debug.Log("[AutoLogin] Editor Chính: Không bật auto login, vui lòng nhập tay.");
            }
        }
    }

// 1. Hàm gọi (Wrapper) để bắt đầu Coroutine
    public void PerformAutoLogin(string email, string pass)
    {
        // Bắt buộc dùng StartCoroutine
        StartCoroutine(AutoLoginCoroutine(email, pass));
    }

// 2. Coroutine xử lý chính
    private IEnumerator AutoLoginCoroutine(string email, string pass)
    {
        Debug.Log($"[AutoLogin] Bắt đầu đăng nhập: {email}...");

        // Gọi Firebase (Async)
        var loginTask = FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, pass);

        // QUAN TRỌNG: Đợi cho đến khi task xong, nhưng vẫn giữ Context ở Main Thread
        yield return new WaitUntil(() => loginTask.IsCompleted);

        // Xử lý khi task xong
        if (loginTask.Exception != null)
        {
            // --- XỬ LÝ LỖI (Giống code mẫu của bạn) ---
            FirebaseException firebaseEx = (FirebaseException)loginTask.Exception.GetBaseException();
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            Debug.LogError($"[AutoLogin] Lỗi: {errorCode}");

            // Bạn có thể update UI lỗi ở đây thoải mái
            // statusTextLoginForm.text = "Lỗi...";
        }
        else
        {
            // --- ĐĂNG NHẬP THÀNH CÔNG ---
            FirebaseUser newUser = loginTask.Result.User;
            Debug.LogFormat("<color=green>[AutoLogin] Thành công: {0}</color>", newUser.Email);

            // --- CHECK BIẾN friend (Thoải mái, không lo lỗi Thread) ---
            if (friend != null)
            {
                Debug.Log($"Friend name: {friend.name}"); // Code này giờ chạy ngon 100%
            }
            else
            {
                Debug.LogWarning("Friend object is null");
            }

            // --- XỬ LÝ LOGIC LOAD SCENE (Clone vs Main) ---
            // Vì đang ở Main Thread, gọi LoadScene trực tiếp, không cần Dispatcher
            CheckAndLoadScene();
        }
    }

    private void CheckAndLoadScene()
    {
        // Logic kiểm tra Clone mà bạn đang làm
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("Đây là Clone -> Load scene Test");
            friend.SwitchTab(0);
            return;
        }
#endif

        // Nếu không phải clone (hoặc bản build thật) -> Load scene Home/Lobby
        Debug.Log("Đây là bản Chính -> Load scene Lobby");
        // loadNextScene(); // Hoặc gọi hàm load của bạn
        friend.SwitchTab(0);
    }
}