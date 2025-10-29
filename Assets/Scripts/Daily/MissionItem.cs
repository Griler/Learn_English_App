using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class MissionItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image missionIcon;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardCoin;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button goButton;
    [SerializeField] private Image statusIcon; // (optional) icon hoàn thành

    private DailyMission missionData;
    private Action<DailyMission> onClaimCallback;


    public void Setup(DailyMission mission, Action<DailyMission> onClaim)
    {
        missionData = mission;
        onClaimCallback = onClaim;

        //if (missionIcon) missionIcon.sprite = mission.title;
        if (descriptionText) descriptionText.text = mission.description;
        if (rewardCoin) rewardCoin.text = $"+{mission.reward}";

        if (claimButton)
        {
            claimButton.interactable = mission.isCompleted && !mission.isClaimed;
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        if (statusIcon)
        {
            if (mission.isClaimed)
                statusIcon.color = Color.yellow; // ví dụ: đã nhận
            else if (mission.isCompleted)
                statusIcon.color = Color.green;  // hoàn thành
            else
                statusIcon.color = Color.gray;   // chưa hoàn thành
        }
    }

    private void OnClaimClicked()
    {
        if (missionData == null || missionData.isClaimed)
            return;

        missionData.isClaimed = true;
        claimButton.interactable = false;

        onClaimCallback?.Invoke(missionData);
    }

    public void Refresh()
    {
        if (claimButton)
            claimButton.interactable = missionData.isCompleted && !missionData.isClaimed;

        if (statusIcon)
        {
            if (missionData.isClaimed)
                statusIcon.color = Color.yellow;
            else if (missionData.isCompleted)
                statusIcon.color = Color.green;
            else
                statusIcon.color = Color.gray;
        }
    }
}
