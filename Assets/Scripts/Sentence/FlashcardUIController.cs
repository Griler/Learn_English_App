using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlashcardUIController : MonoBehaviour
    {
//     public SpacedRepetitionSystem srs; // Kéo object chứa script SRS vào đây
//
//     [Header("UI Elements")]
//     public TextMeshProUGUI ruleText;
//     public TextMeshProUGUI exampleText;
//     public TextMeshProUGUI exerciseQuestionText;
//     public TMP_InputField answerInputField;
//     public Button submitButton;
//     public GameObject feedbackPanel; // Panel chứa các nút đánh giá
//     public TextMeshProUGUI resultText; // Text để báo Đúng/Sai
//
//     private GrammarFlashcard currentCard;
//     private List<GrammarFlashcard> reviewQueue;
//     private int currentCardIndex = 0;
//
//     void Start()
//     {
//         feedbackPanel.SetActive(false);
//         resultText.gameObject.SetActive(false);
//         
//         // Lấy danh sách thẻ cần ôn tập
//         reviewQueue = srs.GetCardsToReviewToday();
//
//         if (reviewQueue.Count > 0)
//         {
//             ShowCard(reviewQueue[0]);
//         }
//         else
//         {
//             ruleText.text = "Bạn đã hoàn thành tất cả các thẻ ôn tập cho hôm nay!";
//             // Vô hiệu hóa các thành phần khác
//         }
//     }
//
//     void ShowCard(GrammarFlashcard card)
//     {
//         currentCard = card;
//         ruleText.text = $"<b>Công thức:</b>\n{card.rule}";
//         exampleText.text = $"<b>Ví dụ:</b>\n<i>{card.example}</i>";
//         exerciseQuestionText.text = $"<b>Bài tập:</b>\n{card.miniExerciseQuestion}";
//
//         // Reset UI
//         answerInputField.text = "";
//         resultText.gameObject.SetActive(false);
//         feedbackPanel.SetActive(false);
//         submitButton.interactable = true;
//     }
//
//     public void OnSubmitAnswer()
//     {
//         string userAnswer = answerInputField.text.Trim();
//         if (userAnswer.Equals(currentCard.miniExerciseAnswer, System.StringComparison.OrdinalIgnoreCase))
//         {
//             resultText.text = "<color=green>Chính xác!</color>";
//         }
//         else
//         {
//             resultText.text = $"<color=red>Sai rồi!</color> Đáp án đúng là: <b>{currentCard.miniExerciseAnswer}</b>";
//         }
//
//         resultText.gameObject.SetActive(true);
//         feedbackPanel.SetActive(true); // Hiển thị các nút đánh giá
//         submitButton.interactable = false;
//     }
//
//     // Gán hàm này cho các nút đánh giá trong Unity Editor
//     public void OnFeedbackButtonPressed(int quality)
//     {
//         srs.UpdateCard(currentCard, quality);
//         
//         // Chuyển sang thẻ tiếp theo
//         currentCardIndex++;
//         if (currentCardIndex < reviewQueue.Count)
//         {
//             ShowCard(reviewQueue[currentCardIndex]);
//         }
//         else
//         {
//             // Hoàn thành
//             ruleText.text = "Tuyệt vời! Bạn đã hoàn thành bài ôn tập hôm nay.";
//             exampleText.text = "";
//             exerciseQuestionText.text = "";
//             answerInputField.gameObject.SetActive(false);
//             submitButton.gameObject.SetActive(false);
//             feedbackPanel.SetActive(false);
//             resultText.gameObject.SetActive(false);
//         }
//     }
}