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
        //Reset dữ liệu user mission: set tất cả nhiệm vụ là chưa hoàn thành
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
        string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("⚠️ Chưa đăng nhập Firebase!");
            return;
        }

        var db = FirebaseDatabase.DefaultInstance;

        // 1️⃣ Tải danh sách nhiệm vụ gốc
        db.GetReference("daily_missions").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted) return;

            DataSnapshot missionSnap = task.Result;
            missions.Clear();

            foreach (var child in missionSnap.Children)
            {
                DailyMission m = new DailyMission
                {
                    id = child.Key,
                    title = child.Child("title").Value.ToString(),
                    description = child.Child("description").Value.ToString(),
                    reward = int.Parse(child.Child("reward").Value.ToString()),
                    isCompleted = false,
                    isClaimed = false
                };
                missions.Add(m);
            }
            db.GetReference($"user_missions/{userId}/missions")
              .GetValueAsync()
              .ContinueWithOnMainThread(userTask =>
              {
                  if (!userTask.IsCompleted) return;

                  DataSnapshot userSnap = userTask.Result;

                  foreach (var m in missions)
                  {
                      var userMission = userSnap.Child(m.id);
                      if (userMission.Exists)
                      {
                          if (userMission.Child("isCompleted").Exists)
                              m.isCompleted = bool.Parse(userMission.Child("isCompleted").Value.ToString());
                          if (userMission.Child("isClaimed").Exists)
                              m.isClaimed = bool.Parse(userMission.Child("isClaimed").Value.ToString());
                      }
                  }
                  CompleteMission(GlobalData.MissionKeys.LOGIN);
                  LoadUserMissionProgress();
              });
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

        for (int i = 0; i < missions.Count; i++)
        {
            GameObject itemObj = Instantiate(missionItemPrefab, contentParent);
            MissionItem item = itemObj.GetComponent<MissionItem>();
            item.Setup(missions[i], OnMissionClaimed);
        }
    }

    private void OnMissionClaimed(DailyMission mission)
    {
        // Xử lý logic khi người chơi nhấn Claim
        Debug.Log($"✅ Nhận {mission.reward} vàng cho nhiệm vụ: {mission.title}");
        mission.isClaimed = true;
        SaveUserMission(mission);
        FirebaseDatabaseManager.Instance.AddCoins(mission.reward);
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
    

    void SaveUserMission(DailyMission m)
    {
        var missionData = new Dictionary<string, object>
        {
            { "isCompleted", m.isCompleted },
            { "isClaimed", m.isClaimed }
        };
        Debug.LogError(m.isClaimed);
        dbRef.Child("user_missions").Child(userId).Child("missions").Child(m.id)
            .UpdateChildrenAsync(missionData);
    }
}
