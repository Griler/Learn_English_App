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
        public const string LEARN_GRAMMAR = "learn_grammar";
        public const string LEARN_VOCA = "learn_vocabulary";
        public const string P2P = "pvp";
        public const string LOGIN = "login";
        //public const string PERFECT_SCORE = "perfect_score";
        public const string LEARN_LISTEN = "learn_listen";
        public const string LEARN_SPEAKING = "learn_speaking";
        public const string WIN_P2P = "win_p2p";
    }
    
    public static class USER
    {
        public static string NAME = "";
        public static string EMAIL = "";
        public static int COIN = 0;
    }
    public static List<ListeningQuestion> questionsToListen;
}