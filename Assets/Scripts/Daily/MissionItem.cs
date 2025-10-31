using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;


public class MissionItem : MonoBehaviour
{
    [Header("UI References")] [SerializeField]
    private Image missionIcon;

    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardCoin;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button goButton;
    [SerializeField] private GameObject statusIcon; // (optional) icon hoàn thành

    private DailyMission missionData;
    private Action<DailyMission> onClaimCallback;


    public void Setup(DailyMission mission, Action<DailyMission> onClaim)
    {
        missionData = mission;
        onClaimCallback = onClaim;

        //if (missionIcon) missionIcon.sprite = mission.title;
        if (descriptionText) descriptionText.text = mission.description;
        if (rewardCoin) rewardCoin.text = $"{mission.reward}";

        if (claimButton)
        {
            bool isActive = mission.isCompleted && !mission.isClaimed;
            claimButton.gameObject.SetActive(isActive);
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        if (goButton)
        {
            bool isActive = !mission.isCompleted;
            goButton.gameObject.SetActive(isActive);
            goButton.onClick.RemoveAllListeners();
            goButton.onClick.AddListener(() =>
            {
                MissionActionManager.Instance.ExecuteMissionAction(missionData.id);
            });
            
        }

        if (statusIcon)
        {
            if (mission.isClaimed)
            {
                statusIcon.SetActive(true);
            }
            else
            {
                statusIcon.SetActive(false);
            }
        }
    }

    private void OnClaimClicked()
    {
        if (missionData == null || missionData.isClaimed)
            return;

        missionData.isClaimed = true;
        statusIcon.gameObject.SetActive(true);
        claimButton.gameObject.SetActive(false);
        onClaimCallback?.Invoke(missionData);
    }

    private void OnGoClicked()
    {
        MissionActionManager.Instance.ExecuteMissionAction(missionData.id);
    }

    
    public void Refresh()
    {
        statusIcon.SetActive(false);
        goButton.gameObject.SetActive(true);
        claimButton.gameObject.SetActive(false);
    }

    void getIconById(string missionKey)
    {
        switch (missionKey)
        {
            case GlobalData.MissionKeys.LEARN_NEW:
                break;
            case GlobalData.MissionKeys.PRACTICE3:
                break;
            case GlobalData.MissionKeys.LOGIN:
                break;
            case GlobalData.MissionKeys.PVP:
                break;
            case GlobalData.MissionKeys.REVIEW:
                break;
            case GlobalData.MissionKeys.PERFECT_SCORE:
                break;
            case GlobalData.MissionKeys.STREAK_3DAYS:
                break;
            default:
                break;
        }
    }
}