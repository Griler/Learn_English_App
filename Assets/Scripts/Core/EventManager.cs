using System;
using UnityEngine;

public static class GameEvents
{
    
    public static event Action<string, Color32?> showNotification;
    public static void ShowNotifcation(string message = "", Color32? color = null)
    {
        showNotification?.Invoke(message,color);
    }
    
    public static event Action showExerciseUI;

    public static void ShowExerciseUI()
    {
        showExerciseUI?.Invoke();
    }

    public static event Action<string> onCompletionMissionDaily;

    public static void CompleteMissionById(string missionId)
    {
        onCompletionMissionDaily?.Invoke(missionId);
    }
    
    public static event Action updateUserInfo;
    
    public static  Action<string,string,string> showInvitePopup;
    
}