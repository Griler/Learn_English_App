using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Database; // Namespace của Realtime DB
using System.Collections.Generic;
using Firebase.Auth;

public class MatchmakingRTDB : MonoBehaviour
{
    public Text statusText;
    public Button findMatchButton;

    private DatabaseReference dbRef;
    private DatabaseReference currentMatchRef; // Tham chiếu đến trận đấu cụ thể

    void Start()
    {
        // Lấy tham chiếu gốc
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        GameDataPVP.MyUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;


        findMatchButton.onClick.AddListener(FindMatch);
    }

    public void FindMatch()
    {
        statusText.text = "Đang quét Realtime DB...";
        findMatchButton.interactable = false;

        // Query: Tìm trong node "matches", sắp xếp theo "status", lấy cái bằng "waiting"
        dbRef.Child("matches").OrderByChild("status").EqualTo("waiting").LimitToFirst(1)
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted) { /* Xử lý lỗi */ return; }

                DataSnapshot snapshot = task.Result;

                if (snapshot.ChildrenCount > 0)
                {
                    // --- CASE 1: TÌM THẤY PHÒNG (JOIN) ---
                    // Lấy phần tử đầu tiên tìm được
                    foreach (var child in snapshot.Children)
                    {
                        JoinMatch(child.Key); 
                        break; // Chỉ cần lấy 1 cái
                    }
                }
                else
                {
                    // --- CASE 2: TẠO PHÒNG MỚI ---
                    CreateMatch();
                }
            });
    }

    void CreateMatch()
    {
        GameDataPVP.MyRole = "player1";

        // Dữ liệu phòng mới
        Dictionary<string, object> newMatch = new Dictionary<string, object>
        {
            {"player1Id", GameDataPVP.MyUserId},
            {"player2Id", ""},
            {"p1Progress", 0},
            {"p2Progress", 0},
            {"status", "waiting"}
        };

        // Push() tạo ra một Key ngẫu nhiên (dạng -Nx...)
        DatabaseReference newRoomRef = dbRef.Child("matches").Push();
        GameDataPVP.CurrentMatchId = newRoomRef.Key;
        currentMatchRef = newRoomRef;

        newRoomRef.SetValueAsync(newMatch).ContinueWith(task => {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                statusText.text = "Đã tạo phòng. Chờ người chơi...";
            });
            
            // Lắng nghe sự kiện thay đổi giá trị của phòng này
            currentMatchRef.ValueChanged += OnMatchStatusChanged;
        });
    }

    void JoinMatch(string matchId)
    {
        GameDataPVP.MyRole = "player2";
        GameDataPVP.CurrentMatchId = matchId;
        currentMatchRef = dbRef.Child("matches").Child(matchId);

        // Cập nhật thông tin: Vào phòng và đổi status thành playing
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            {"player2Id", GameDataPVP.MyUserId},
            {"status", "playing"}
        };

        currentMatchRef.UpdateChildrenAsync(updates).ContinueWith(task => {
            // Vào game luôn
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                GoToGameScene();
            });
        });
    }

    // Hàm lắng nghe (dành cho chủ phòng)
    void OnMatchStatusChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        // Lấy status mới
        string status = args.Snapshot.Child("status").Value.ToString();

        if (status == "playing")
        {
            // Hủy lắng nghe trước khi chuyển scene để tránh lỗi
            currentMatchRef.ValueChanged -= OnMatchStatusChanged;

            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                GoToGameScene();
            });
        }
    }

    void GoToGameScene()
    {
        SceneManager.LoadScene("Test");
    }
}