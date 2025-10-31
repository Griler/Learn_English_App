using UnityEngine;

public class MissionActionManager : MonoBehaviour
{
    public static MissionActionManager Instance;
    public UIManager UIManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ExecuteMissionAction(string missionId)
    {
        switch (missionId)
        {
            case GlobalData.MissionKeys.LEARN_NEW:
                OpenLessonSelectView();
                break;

            case GlobalData.MissionKeys.REVIEW:
                OpenReviewView();
                break;

            case GlobalData.MissionKeys.PVP:
                OpenPvPMode();
                break;

            case GlobalData.MissionKeys.PRACTICE3:
                OpenPracticeView();
                break;

            default:
                Debug.Log($"❓Không có hành động cho mission {missionId}");
                break;
        }
    }

    void OpenLessonSelectView()
    {
        Debug.Log("👉 Mở màn hình chọn bài học mới");
        UIManager.OpenViewByMissionid(GlobalData.MissionKeys.LEARN_NEW);
    }

    void OpenReviewView()
    {
        Debug.Log("👉 Mở màn hình ôn tập");
        UIManager.OpenViewByMissionid(GlobalData.MissionKeys.REVIEW);
    }

    void OpenPvPMode()
    {
        Debug.Log("👉 Chuyển sang chế độ PvP");
        UIManager.OpenViewByMissionid(GlobalData.MissionKeys.PVP);
    }

    void OpenPracticeView()
    {
        Debug.Log("👉 Mở giao diện luyện tập");
        UIManager.OpenViewByMissionid(GlobalData.MissionKeys.PRACTICE3);
    }
}