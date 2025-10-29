using System;
using UnityEngine;

[Serializable]
public class DailyMission
{
    public string id;
    public string title;
    public string description;
    public int reward;
    public bool isCompleted;
    public bool isClaimed;
}