using System.Collections.Generic;
using UnityEngine;

public static class GlobalData
{
    public static string selectedNameSO = "Pet";
    public static string homeScene = "HomeScene";
    public static string flashCardScene = "FlashCardScene";
    public static string loginScene =  "LoginScene";
    public static string pairScene =  "PairScene";
    public static string pathData = "Data";
    public static string pathAnimalData = "AnimalData";
    public static string pathUserInfoData = "UserData";
    public static string pathGramaData = "GramaData";
    
    public static class MissionKeys
    {
        public const string LEARN_NEW = "learn_new";
        public const string PRACTICE3 = "practice3";
        public const string LOGIN = "login";
        public const string PVP = "pvp";
        public const string REVIEW = "review";
        public const string PERFECT_SCORE = "perfect_score";
        public const string STREAK_3DAYS = "streak_3days";
    }
    
    public static class USER
    {
        public static string NAME = "";
        public static string EMAIL = "";
        public static int COIN = 0;
    }
    public static List<ListeningQuestion> questionsToListen;
}