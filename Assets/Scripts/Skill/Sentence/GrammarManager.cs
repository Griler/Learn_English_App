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

    public void GetCardsToLearn(Action<List<GrammarExample>> cb)  
    {
        int grammarCategoryId = PlayerPrefs.GetInt("SelectedGrammarTopic");
        StartCoroutine(ApiController.Instance.GetGrammarExamByCategoryId(grammarCategoryId, cb , 1));
    }

    public void GetCardsToWrite(Action<List<GrammarExercise>> cb)
    {
        int grammarCategoryId = PlayerPrefs.GetInt("SelectedGrammarTopic");
        StartCoroutine(ApiController.Instance.GetGrammarExercisesByCategoryId(grammarCategoryId, cb));
    }
}