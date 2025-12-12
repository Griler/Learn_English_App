using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingView : MonoBehaviour
{
    public TextMeshProUGUI userId;
    public Button coppyButton;
    public Button signOutButton;

    private void OnEnable()
    {
        userId.text = FirebaseDatabaseManager.Instance.currentUser.UserId.ToString();
        coppyButton.onClick.AddListener(()=>
        {
            CopyToClipboard(userId.text);
        });
        signOutButton.onClick.AddListener((() =>
        {
            FirebaseDatabaseManager.Instance.fireAuthReference.SignOut();
            SceneManager.LoadScene("LoginScene");
        }));
    }

    private void OnDisable()
    {
        coppyButton.onClick.RemoveAllListeners();
        signOutButton.onClick.RemoveAllListeners();

    }

    // Hàm này bạn có thể gắn vào sự kiện OnClick của Button
    public void CopyToClipboard(string textToCopy)
    {
        // Đây là lệnh quan trọng nhất
        GUIUtility.systemCopyBuffer = textToCopy;
        ToastSystem.Instance.ShowToast("Đã copy vào clipboard!!!");
        // Log kiểm tra (hoặc hiển thị thông báo "Đã copy" cho người dùng)
        Debug.Log("Đã copy vào clipboard: " + textToCopy);
    }
}