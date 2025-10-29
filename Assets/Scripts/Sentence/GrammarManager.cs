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
    private int totalFlashcards = 0;
    private int totalFlashcardsExample = 0;
    private int totalFlashcardsExercise = 0;

    void Awake()
    {
        InitializeExmampleData();
    }

    private void OnEnable()
    {
        //EventManager.On(EventCode.SHOW_EXERCISE_UI,InitializeExerciseData);
    }

    private void OnDisable()
    {
        //EventManager.Off(EventCode.SHOW_EXERCISE_UI,InitializeExerciseData);
    }

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
    private void InitializeExmampleData()
    {
        loaded = Resources.LoadAll<GrammarData>(pathLoad);
        listExercise.AddRange(FromGrammarData(loaded[0]));
        listExample.AddRange(loaded[0].examples);
        totalFlashcards = Random.Range(10, 12);
        totalFlashcardsExample = listExample.Count;
    }

    private void InitializeExerciseData()
    {
        Debug.LogError("ds");
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
        int randomCardToLearn =
            Random.Range(Convert.ToInt32(totalFlashcards / 2) - 1, Convert.ToInt32(totalFlashcards / 2));

        List<GrammarFlashcardExmpale> shuffledList = new List<GrammarFlashcardExmpale>(listExample);
        int count = shuffledList.Count;

        if (count < randomCardToLearn)
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

        if (count < randomCardToLearn)
        {
            randomCardToLearn = count;
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