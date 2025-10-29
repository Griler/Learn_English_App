using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class DailyMissionManager : MonoBehaviour
{
    [SerializeField] private GameObject missionItemPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private string userId = "..."; // sau này có thể thay bằng FirebaseAuth.UserId

    private DatabaseReference dbRef;
    private List<DailyMission> missions = new List<DailyMission>();

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                LoadMissionsWithResetCheck();
            }
            else
            {
                Debug.LogError("Không thể khởi tạo Firebase: " + dependencyStatus);
            }
        });
    }

    void LoadMissionsWithResetCheck()
    {
        userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        DatabaseReference userRef = FirebaseDatabase.DefaultInstance.GetReference("user_missions").Child(userId);
        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                // Nếu user chưa có data hoặc ngày khác ngày hôm nay → reset nhiệm vụ
                if (!snapshot.Exists || !snapshot.HasChild("last_reset_date") ||
                    snapshot.Child("last_reset_date").Value.ToString() != today)
                {
                    Debug.Log("Reset nhiệm vụ mới cho ngày " + today);
                    ResetUserDailyMissions(today);
                }
                else
                {
                    // Load danh sách nhiệm vụ
                    LoadMissionsFromServer();
                }
            }
        });
    }

    void ResetUserDailyMissions(string today)
    {
        // Reset dữ liệu user mission: set tất cả nhiệm vụ là chưa hoàn thành
        FirebaseDatabase.DefaultInstance
            .GetReference("daily_missions")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted) return;
                DataSnapshot snapshot = task.Result;

                Dictionary<string, object> resetData = new Dictionary<string, object>();
                Dictionary<string, object> missionStates = new Dictionary<string, object>();

                foreach (var child in snapshot.Children)
                {
                    string id = child.Key;
                    missionStates[id] = new Dictionary<string, object>
                    {
                        { "isCompleted", false },
                        { "isClaimed", false }
                    };
                }

                resetData["last_reset_date"] = today;
                resetData["missions"] = missionStates;

                dbRef.Child("user_missions").Child(userId).SetValueAsync(resetData)
                    .ContinueWithOnMainThread(t =>
                    {
                        Debug.Log("✅ Reset nhiệm vụ thành công");
                        LoadMissionsFromServer();
                    });
            });
    }

    void LoadMissionsFromServer()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("daily_missions")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    missions.Clear();

                    foreach (var child in snapshot.Children)
                    {
                        DailyMission m = new DailyMission();
                        m.id = child.Key;
                        m.title = child.Child("title").Value.ToString();
                        m.description = child.Child("description").Value.ToString();
                        m.reward = int.Parse(child.Child("reward").Value.ToString());
                        m.isCompleted = false;
                        m.isClaimed = false;
                        missions.Add(m);
                    }

                    LoadUserMissionProgress();
                }
            });
    }

    void LoadUserMissionProgress()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("user_missions").Child(userId).Child("missions")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    DataSnapshot snapshot = task.Result;

                    foreach (var mission in missions)
                    {
                        if (snapshot.HasChild(mission.id))
                        {
                            var userData = snapshot.Child(mission.id);
                            mission.isCompleted = userData.Child("isCompleted").Value as bool? ?? false;
                            mission.isClaimed = userData.Child("isClaimed").Value as bool? ?? false;
                        }
                    }
                }
                DisplayMissions();
            });
    }

    void DisplayMissions()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var mission in missions)
        {
            GameObject item = Instantiate(missionItemPrefab, contentParent);
            item.transform.Find("Title").GetComponent<Text>().text = mission.title;
            item.transform.Find("Description").GetComponent<Text>().text = mission.description;
            item.transform.Find("Reward").GetComponent<Text>().text = $"+{mission.reward}";

            Button claimBtn = item.transform.Find("ClaimButton").GetComponent<Button>();
            claimBtn.interactable = mission.isCompleted && !mission.isClaimed;
            claimBtn.onClick.AddListener(() => ClaimMission(mission, claimBtn));
        }
    }

    public void CompleteMission(string missionId)
    {
        DailyMission m = missions.Find(x => x.id == missionId);
        if (m != null)
        {
            m.isCompleted = true;
            SaveUserMission(m);
            DisplayMissions();
        }
    }

    void ClaimMission(DailyMission mission, Button btn)
    {
        if (mission.isClaimed) return;
        mission.isClaimed = true;
        btn.interactable = false;

        SaveUserMission(mission);

        Debug.Log($"✅ Nhận {mission.reward} vàng cho nhiệm vụ: {mission.title}");
        // TODO: Cộng vàng vào tài khoản người chơi
    }

    void SaveUserMission(DailyMission m)
    {
        var missionData = new Dictionary<string, object>
        {
            { "isCompleted", m.isCompleted },
            { "isClaimed", m.isClaimed }
        };

        dbRef.Child("user_missions").Child(userId).Child("missions").Child(m.id)
            .UpdateChildrenAsync(missionData);
    }
}
