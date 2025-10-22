using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GrammarManager : MonoBehaviour
{
    // Giả sử bạn có một danh sách tất cả các thẻ
    public List<GrammarFlashcardExercise> listExercise = new List<GrammarFlashcardExercise>();
    public List<GrammarFlashcardExmpale> listExample;
    [SerializeField] private GrammarData[] loaded;
    private string pathLoad = $"{GlobalData.pathData}/{GlobalData.pathGramaData}";
    // Dictionary để theo dõi điểm yếu
    // Key: grammarPointID, Value: số lần trả lời sai
    public Dictionary<string, int> performanceTracker = new Dictionary<string, int>();

    void Awake()
    {
        InitializeSampleData();
    }

    // Lấy danh sách các thẻ cần ôn tập ngay hôm nay
    public List<GrammarFlashcardExercise> GetCardsToReviewToday()
    {
        List<GrammarFlashcardExercise> reviewQueue = new List<GrammarFlashcardExercise>();
        foreach (var card in listExercise)
        {
            if (card.nextReviewDate <= DateTime.UtcNow)
            {
                reviewQueue.Add(card);
            }
        }
        return reviewQueue;
    }

    /// <summary>
    /// Cập nhật một thẻ dựa trên chất lượng câu trả lời của người dùng.
    /// </summary>
    /// <param name="card">Thẻ được cập nhật.</param>
    /// <param name="quality">Chất lượng trả lời (0: Sai hoàn toàn, 3: Khó, 4: Bình thường, 5: Dễ).</param>
    // public void UpdateCard(GrammarFlashcardExercise card, int quality)
    // {
    //     if (quality < 3)
    //     {
    //         // Trả lời sai -> Reset interval và theo dõi lỗi
    //         card.interval = 1; // Ôn lại vào ngày mai
    //         TrackWeakness(card.grammarPointID);
    //     }
    //     else
    //     {
    //         // Cập nhật Ease Factor
    //         card.easeFactor = Mathf.Max(1.3f, card.easeFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f)));
    //
    //         // Cập nhật Interval
    //         if (card.interval == 0) card.interval = 1;
    //         else if (card.interval == 1) card.interval = 6;
    //         else
    //         {
    //             card.interval = Mathf.RoundToInt(card.interval * card.easeFactor);
    //         }
    //     }
    //     
    //     // Đặt ngày ôn tập tiếp theo
    //     card.nextReviewDate = DateTime.UtcNow.AddDays(card.interval);
    //
    //     Debug.Log($"Card '{card.grammarPointID}' updated. New Interval: {card.interval} days. New Ease: {card.easeFactor}. Next review: {card.nextReviewDate}");
    //
    //     // TODO: Lưu lại tiến trình của thẻ này
    // }
    
    // Theo dõi điểm yếu
    private void TrackWeakness(string grammarPointID)
    {
        if (performanceTracker.ContainsKey(grammarPointID))
        {
            performanceTracker[grammarPointID]++;
        }
        else
        {
            performanceTracker.Add(grammarPointID, 1);
        }
    }

    // Lấy danh sách các điểm ngữ pháp yếu nhất
    public List<string> GetWeakestGrammarPoints(int count)
    {
        // Sắp xếp dictionary theo số lần sai giảm dần
        List<KeyValuePair<string, int>> sortedTracker = new List<KeyValuePair<string, int>>(performanceTracker);
        sortedTracker.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
        
        List<string> weakestPoints = new List<string>();
        for (int i = 0; i < Mathf.Min(count, sortedTracker.Count); i++)
        {
            weakestPoints.Add(sortedTracker[i].Key);
        }
        return weakestPoints;
    }

    // Tạo dữ liệu mẫu để thử nghiệm
    private void InitializeSampleData()
    {
        Debug.LogError(pathLoad);
        loaded = Resources.LoadAll<GrammarData>(pathLoad);
        listExercise.AddRange(FromGrammarData(loaded[0]));
        listExample.AddRange(loaded[0].examples);
        Debug.Log(loaded.Length);
    }


    public List<GrammarFlashcardExercise> FromGrammarData(GrammarData grammarData)
    {
        var flashcards = new List<GrammarFlashcardExercise>();

        foreach (var ex in grammarData.miniExercises)
        {
            var card = new GrammarFlashcardExercise(
                ex.question,
                ex.answer,
                ex.difficultyLevel,
                grammarData.grammarPointID
            );
            flashcards.Add(card);
        }

        return flashcards;
    }

    public List<GrammarFlashcardExmpale> GetCardsToLearn()
    {
        int randomCardToLearn = Random.Range(8, 11);

        List<GrammarFlashcardExmpale> shuffledList = new List<GrammarFlashcardExmpale>(listExample);
        int count = shuffledList.Count;

        if (count <  randomCardToLearn)
        {
            randomCardToLearn = count; // Giới hạn số lượng bằng kích thước của danh sách
        }

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GrammarFlashcardExmpale temp = shuffledList[i];
            shuffledList[i] = shuffledList[j];
            shuffledList[j] = temp;
        }
        
        return shuffledList.GetRange(0, randomCardToLearn);
    }
    
    public List<GrammarFlashcardExercise> GetCardsToWrite()
    {
        int randomCardToLearn = Random.Range(8, 11);

        List<GrammarFlashcardExercise> shuffledList = new List<GrammarFlashcardExercise>(listExercise);
        int count = shuffledList.Count;

        if (count <  randomCardToLearn)
        {
            randomCardToLearn = count; // Giới hạn số lượng bằng kích thước của danh sách
        }

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GrammarFlashcardExercise temp = shuffledList[i];
            shuffledList[i] = shuffledList[j];
            shuffledList[j] = temp;
        }
        
        return shuffledList.GetRange(0, randomCardToLearn);
    }
}