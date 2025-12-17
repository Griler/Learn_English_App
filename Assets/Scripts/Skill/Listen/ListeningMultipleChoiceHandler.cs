using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListeningMultipleChoiceHandler : MonoBehaviour
{
    [Header("--- UI CONFIG ---")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;
    
    [Header("--- COLORS ---")]
    public Color clickColor = Color.yellow;
    public Color wrongColor = Color.red;
    public Color correctColor = Color.green;
    public Color defaultColor = Color.white;

    private ListeningQuestion currentQ;
    private GameObject currentButtonClick;
    private string chosenAnswer = "";

    // Hàm khởi tạo giao diện cho câu hỏi hiện tại
    public void Setup(ListeningQuestion q)
    {
        currentQ = q;
        choiceContainer.SetActive(true);
        ResetUI();
        setInteractable(true);

        // Tạo list đáp án (Đúng + Sai)
        List<string> options = new List<string> { q.correctAnswer };
        if (q.wrongAnswers.Count > 0 )
        {
            // Tách chuỗi các đáp án sai (phân cách bởi dấu phẩy) và thêm vào danh sách
            for (int i = 0; i <  q.wrongAnswers.Count; i++)
            {
                options.Add(q.wrongAnswers[i]);
            }
        }

        // Trộn đáp án
        options = options.OrderBy(x => Random.value).ToList();

        // Hiển thị lên nút
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i];
                
                // Add Listener
                GameObject btnObj = choiceButtons[i].gameObject;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnClickAnswer(btnObj));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnClickAnswer(GameObject btnObj)
    {
        // Reset màu nút cũ
        if (currentButtonClick != null)
        {
            currentButtonClick.GetComponent<Image>().color = defaultColor;
        }

        // Highlight nút mới
        currentButtonClick = btnObj;
        chosenAnswer = btnObj.GetComponentInChildren<TextMeshProUGUI>().text;
        currentButtonClick.GetComponent<Image>().color = clickColor;

        // Báo cho Manager biết là đã chọn xong (để bật nút Next nếu cần)
        ListeningGameManager.Instance.OnAnswerSelected();
    }

    // Kiểm tra đúng sai và trả về kết quả
    public bool CheckAnswerAndShowFeedback()
    {
        if (currentButtonClick == null) return false;

        bool isCorrect = chosenAnswer == currentQ.correctAnswer;

        if (isCorrect)
        {
            currentButtonClick.GetComponent<Image>().color = correctColor;
        }
        else
        {
            currentButtonClick.GetComponent<Image>().color = wrongColor;
        }

        return isCorrect;
    }

    public void Hide()
    {
        choiceContainer.SetActive(false);
    }

    public void ResetUI()
    {
        currentButtonClick = null;
        chosenAnswer = "";
        foreach (Button btn in choiceButtons)
        {
            btn.GetComponent<Image>().color = defaultColor;
        }
    }
    
    // Để reset màu về default sau khi hiển thị sai (nếu cần chơi lại ngay)
    public void ResetColorCurrentButton()
    {
        if (currentButtonClick != null)
            currentButtonClick.GetComponent<Image>().color = defaultColor;
    }

    public void setInteractable(bool active)
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].interactable = active ;
            
        }
    }
}