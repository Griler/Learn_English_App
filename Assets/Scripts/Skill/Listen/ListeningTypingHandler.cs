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
    private bool isAdd = false;

    private void Start()
    {
        submitTypingBtn.onClick.AddListener(CheckTypingAnswer);
    }

    public void Setup(ListeningQuestion q, Action onNext, Action onSkip )
    {
        isAdd = false;
        currentQ = q;
        inputField.text = "";
        this.onNextCallBack = onNext;
        onSkipCallback = onSkip;
        typingContainer.SetActive(true);
        submitTypingBtn.interactable = true;
        resultText.text = "";
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
        resultText.text = "Đúng";
        resultText.color = Color.green;
        yield return new WaitForSeconds(0.5f);
        ListeningGameManager.Instance.answerChoose[currentQ.correctAnswer] = true;
        onNextCallBack?.Invoke();
    }

    IEnumerator ProcessWrongAnswer()
    {
        resultText.text = "Sai";
        resultText.color = Color.softRed;
        ListeningGameManager.Instance.answerChoose[currentQ.correctAnswer] = false;
        yield return new WaitForSeconds(0.5f);
        resultText.text = "";
        resultText.color = Color.white;
    }

    public void Hide()
    {
        typingContainer.SetActive(false);
    }
}