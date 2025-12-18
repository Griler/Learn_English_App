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
        UIManager.OpenViewByMissionid(missionId);
    }

    void OpenLessonSelectView()
    {
        Debug.Log("👉 Mở màn hình chọn bài học mới");
    }

    void OpenReviewView()
    {
        Debug.Log("👉 Mở màn hình ôn tập");
        UIManager.OpenViewByMissionid(GlobalData.MissionKeys.P2P);
    }

    void OpenPvPMode()
    {
        Debug.Log("👉 Chuyển sang chế độ PvP");
        //UIManager.OpenViewByMissionid(GlobalData.MissionKeys.PVP);
    }

    void OpenPracticeView()
    {
        Debug.Log("👉 Mở giao diện luyện tập");
    }
}