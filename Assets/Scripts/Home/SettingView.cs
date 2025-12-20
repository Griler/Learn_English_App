using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingView : MonoBehaviour
{
    public TextMeshProUGUI userId;
    public TextMeshProUGUI nameUser;
    public TextMeshProUGUI nameInputField;
    public Button saveButton;
    public Button cancelButton;
    public Button changeButton;
    public GameObject panelInputName;
    public Button coppyButton;
    public Button signOutButton;

    private string USER_ID = "";
    private void OnEnable()
    {
        USER_ID = FirebaseDatabaseManager.Instance.currentUser.UserId;
        userId.text = USER_ID.Substring(0,5) + "....." + USER_ID.Substring(USER_ID.Length - 5);
        nameUser.text = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.name;
        coppyButton.onClick.AddListener(()=>
        {
            CopyToClipboard();
        });
        signOutButton.onClick.AddListener((() =>
        {
            FirebaseDatabaseManager.Instance.fireAuthReference.SignOut();
            SceneManager.LoadScene("LoginScene");
        }));
        
        changeButton.onClick.AddListener(() =>
        {
            panelInputName.SetActive(true);
        });
        
        cancelButton.onClick.AddListener(() =>
        {
            nameInputField.text = "";
            panelInputName.SetActive(false);
        });
        
        saveButton.onClick.AddListener((() =>
        {
            StartCoroutine(saveName());
        }));
    }

    private void OnDisable()
    {
        coppyButton.onClick.RemoveAllListeners();
        signOutButton.onClick.RemoveAllListeners();
        changeButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        saveButton.onClick.RemoveAllListeners();
        panelInputName.SetActive(false);
    }

    // Hàm này bạn có thể gắn vào sự kiện OnClick của Button
    public void CopyToClipboard()
    {
        // Đây là lệnh quan trọng nhất
        GUIUtility.systemCopyBuffer = USER_ID;
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
}