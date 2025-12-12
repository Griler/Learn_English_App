using UnityEngine;
using TMPro;                // Thư viện TextMeshPro
using UnityEngine.InputSystem; // Thư viện Input System mới

public class FixAndroidBackspace : MonoBehaviour
{
    private TMP_InputField inputField;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    void Update()
    {
        // Chỉ xử lý khi người dùng đang bấm vào ô Input này
        if (inputField != null && inputField.isFocused)
        {
            // Kiểm tra bàn phím hiện tại có tồn tại không
            if (Keyboard.current != null)
            {
                // Bắt sự kiện nút Backspace vừa được nhấn trong frame này
                if (Keyboard.current.backspaceKey.wasPressedThisFrame)
                {
                    ManualDelete();
                }
            }
        }
    }

    void ManualDelete()
    {
        // Nếu không có chữ thì thôi
        if (string.IsNullOrEmpty(inputField.text)) return;

        // Lấy vị trí con trỏ hiện tại
        int caretPos = inputField.caretPosition;

        // Nếu con trỏ đang ở đầu dòng (vị trí 0) thì không có gì bên trái để xóa
        if (caretPos > 0)
        {
            // Xóa 1 ký tự tại vị trí trước con trỏ
            inputField.text = inputField.text.Remove(caretPos - 1, 1);

            // Cập nhật lại vị trí con trỏ lùi lại 1 nấc (để không bị nhảy về cuối)
            inputField.caretPosition = caretPos - 1;
            
            // Ép InputField cập nhật lại giao diện ngay lập tức
            inputField.ForceLabelUpdate();
        }
    }
}