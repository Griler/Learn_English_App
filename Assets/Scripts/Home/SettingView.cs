using System;
using System.Collections;
using System.Text.RegularExpressions;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingView : MonoBehaviour
{
    public TextMeshProUGUI userId;
    public TextMeshProUGUI nameUser;
    public TMP_InputField nameInputField;
    public TextMeshProUGUI emailText;
    public Button saveButton;
    public Button cancelButton;
    public Button changeNameButton;
    public Button changeMailButton;
    public GameObject panelInputName;
    public Button coppyButton;
    public Button signOutButton;
    public Button linkEmailButton;
    public Button saveEmailButton;
    public string typeChange = "";
    private string USER_ID = "";
    private void OnEnable()
    {
        USER_ID = FirebaseDatabaseManager.Instance.currentUser.UserId;
        userId.text = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.username;
        nameUser.text = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.name;
        emailText.text = FirebaseDatabaseManager.Instance.currentUser.Email;
        
        coppyButton.onClick.AddListener(()=>
        {
            CopyToClipboard();
        });
        signOutButton.onClick.AddListener((() =>
        {
            FirebaseDatabaseManager.Instance.fireAuthReference.SignOut();
            SceneManager.LoadScene("LoginScene");
        }));
        
        changeNameButton.onClick.AddListener(() =>
        {
            typeChange = "name";
            panelInputName.SetActive(true);
        });   
        changeMailButton.onClick.AddListener(() =>
        {
            typeChange = "email";
            panelInputName.SetActive(true);
        });
        
        cancelButton.onClick.AddListener(() =>
        {
            nameInputField.text = "";
            panelInputName.SetActive(false);
        });
        
        saveButton.onClick.AddListener((() =>
        {
            if (typeChange == "name")
            {
                StartCoroutine(saveName());
            }
            else if (typeChange == "email")
            {
                saveNewEmail();
            }
        }));
        if (FirebaseDatabaseManager.Instance.fireAuthReference.CurrentUser != null 
            && FirebaseDatabaseManager.Instance.fireAuthReference.CurrentUser.IsAnonymous)
        {
            linkEmailButton.interactable = true;
            changeMailButton.interactable = false;
        }
        else
        {
            linkEmailButton.interactable = false;
            changeMailButton.interactable = true;
        }
    }

    private void OnDisable()
    {
        coppyButton.onClick.RemoveAllListeners();
        signOutButton.onClick.RemoveAllListeners();
        changeMailButton.onClick.RemoveAllListeners();
        changeNameButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        saveButton.onClick.RemoveAllListeners();
        panelInputName.SetActive(false);
    }

    // Hàm này bạn có thể gắn vào sự kiện OnClick của Button
    public void CopyToClipboard()
    {
        // Đây là lệnh quan trọng nhất
        GUIUtility.systemCopyBuffer = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.username;
        ToastSystem.Instance.ShowToast("Đã copy vào clipboard!!!");
        // Log kiểm tra (hoặc hiển thị thông báo "Đã copy" cho người dùng)
        Debug.Log("Đã copy vào clipboard: ");
    }

    IEnumerator saveName()
    {
        if (string.IsNullOrEmpty(nameInputField.text))
        {
            ToastSystem.Instance.ShowToast("Vui lòng nhập tên");
        }
        else if (nameInputField.text.Length > 10)
        {
            ToastSystem.Instance.ShowToast("Vui lòng nhập tên dưới 10 ký tự");
        }
        else
        {
            var task = FirebaseDatabaseManager.Instance.dbReference
                .Child("users")
                .Child(USER_ID)
                .Child("userInfo").Child("name")
                .SetValueAsync(nameInputField.text);
        
            yield return new WaitUntil(() => task.IsCompleted);
            ToastSystem.Instance.ShowToast("Bạn đổi tên thành công");
            nameUser.text = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.name;
            panelInputName.SetActive(false);
        }
    }
    
    public void saveNewEmail()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null) return;
        string correctedEmail = nameInputField.text.Trim();

        if (!IsValidEmail(correctedEmail))
        {
            ToastSystem.Instance.ShowToast("Email không phù hợp vui lòng thử lại");
            return;
        }
        
        if (correctedEmail == user.Email)
        {
            ToastSystem.Instance.ShowToast("Email này y hệt cái cũ mà?");
            return;
        }

        user.SendEmailVerificationBeforeUpdatingEmailAsync(correctedEmail).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled) return;

            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi update mail: " + task.Exception);
                FirebaseException firebaseEx = (FirebaseException)task.Exception.GetBaseException();
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Cập nhật thất bại: ";
                switch (errorCode)
                {
                    case AuthError.InvalidEmail:
                        message += "Email không hợp lệ.";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message += "Email đã được dùng";
                        break;
                }
                ToastSystem.Instance.ShowToast(message);
                return;
            }
            Debug.Log("Đã sửa lại email thành công: " + correctedEmail);
            ToastSystem.Instance.ShowToast("Vui lòng kiểm tra mail và xác nhận để hoàn thành");
            user.ReloadAsync();
            Debug.Log("email mới: " + user.Email);
        });
    }
    
    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        // Regex pattern cho email
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        
        return Regex.IsMatch(email, pattern);
    }
}