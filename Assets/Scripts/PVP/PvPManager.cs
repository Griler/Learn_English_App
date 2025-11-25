using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;

public class GamePlayRTDB : MonoBehaviour
{
    [Header("UI")]
    public Slider myProgressBar;
    public Slider enemyProgressBar;
    public Text infoText;
    public Button correctAnswerBtn;

    private DatabaseReference matchRef;

    void Start()
    {
        // Tham chiếu thẳng vào ID trận đấu hiện tại
        matchRef = FirebaseDatabase.DefaultInstance
            .GetReference("matches")
            .Child(GameDataPVP.CurrentMatchId);

        infoText.text = "Role: " + GameDataPVP.MyRole;
        
        correctAnswerBtn.onClick.AddListener(SubmitCorrectAnswer);

        // Bắt đầu lắng nghe thay đổi dữ liệu (Sync)
        matchRef.ValueChanged += HandleMatchUpdates;
    }

    // Hàm này chạy mỗi khi dữ liệu trên DB thay đổi
    void HandleMatchUpdates(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        DataSnapshot snapshot = args.Snapshot;

        // Lấy giá trị (Convert sang long/int an toàn)
        long p1 = (long)snapshot.Child("p1Progress").Value;
        long p2 = (long)snapshot.Child("p2Progress").Value;

        // Cập nhật UI trên Main Thread
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            UpdateUI(p1, p2);
        });
    }

    void UpdateUI(long p1, long p2)
    {
        if (GameDataPVP.MyRole == "player1")
        {
            myProgressBar.value = p1;
            enemyProgressBar.value = p2;
        }
        else
        {
            myProgressBar.value = p2;
            enemyProgressBar.value = p1;
        }

        if (myProgressBar.value >= 10) infoText.text = "WINNER!";
        else if (enemyProgressBar.value >= 10) infoText.text = "LOSER!";
    }

    void SubmitCorrectAnswer()
    {
        // Xác định đường dẫn con cần tăng điểm (p1Progress hoặc p2Progress)
        string path = (GameDataPVP.MyRole == "player1") ? "p1Progress" : "p2Progress";
        
        // Dùng Transaction để cộng điểm an toàn (Atomic Increment)
        matchRef.Child(path).RunTransaction(mutableData =>
        {
            // Lấy giá trị hiện tại
            object val = mutableData.Value;
            long currentScore = (val == null) ? 0 : (long)val;
            
            // Ghi đè giá trị mới (Cộng 1)
            mutableData.Value = currentScore + 1;
            
            return TransactionResult.Success(mutableData);
        });
    }

    void OnDestroy()
    {
        // Cực kỳ quan trọng: Hủy đăng ký sự kiện khi thoát scene/game
        if (matchRef != null) matchRef.ValueChanged -= HandleMatchUpdates;
    }
}