using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListeningTypingHandler : MonoBehaviour
{
    [Header("--- UI CONFIG ---")]
    public GameObject typingContainer;
    public TMP_InputField inputField;
    public Button submitTypingBtn;
    public TextMeshProUGUI resultText;

    private ListeningQuestion currentQ;
    private Action onNextCallBack;
    private Action onSkipCallback;

    private void Start()
    {
        submitTypingBtn.onClick.AddListener(CheckTypingAnswer);
    }

    public void Setup(ListeningQuestion q, Action onNext, Action onSkip )
    {
        currentQ = q;
        this.onNextCallBack = onNext;
        onSkipCallback = onSkip;
        typingContainer.SetActive(true);
        submitTypingBtn.interactable = true;
        resultText.text = "";
        // Gọi script bàn phím ảo (đảm bảo typingContainer có script này)
        try
        {
            var keyboard = typingContainer.GetComponent<InputKeyBoardCustom>();
            if (keyboard != null)
            {
                keyboard.initButtonWord(q.correctAnswer);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Lỗi khởi tạo bàn phím: " + e.Message);
        }
    }

    void CheckTypingAnswer()
    {
        string userInput = inputField.text.Trim().ToLower();
        string correct = currentQ.correctAnswer.Trim().ToLower();

        if (userInput == correct)
        {
            StartCoroutine(ProcessCorrectAnswer());
        }
        else
        {
            StartCoroutine(ProcessWrongAnswer());
        }
    }
    
    
    IEnumerator ProcessCorrectAnswer()
    {
        resultText.text = "Correct";
        resultText.color = Color.green;
        yield return new WaitForSeconds(0.5f); // Đợi một chút để người dùng thấy kết quả
        onNextCallBack?.Invoke();
    }

    IEnumerator ProcessWrongAnswer()
    {
        resultText.text = "Wrong";
        resultText.color = Color.softRed;
        yield return new WaitForSeconds(0.5f);
        resultText.text = "";
        resultText.color = Color.white;
    }

    public void Hide()
    {
        typingContainer.SetActive(false);
    }
}