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
    public static class STATUS
    {
        public const string ONLINE = "ONLINE";
        public const string OFFLINE = "OFFLINE";
        public const string INMATCH = "INMATCH";
    }
}

// PVPRandom.cs - Không cần gắn vào GameObject
public class PVPRandom
{
    private uint _state;

    // Khởi tạo với Seed
    public PVPRandom(int seed)
    {
        // Ép kiểu sang uint, tránh số âm gây lỗi logic
        _state = (uint)seed;
        // Nếu seed = 0 thì đổi thành 1 (thuật toán này ghét số 0)
        if (_state == 0) _state = 1;
    }

    // Hàm lấy số tiếp theo (thay thế Next() của System.Random)
    public int Next()
    {
        // Công thức toán học cố định: không bao giờ thay đổi theo nền tảng
        _state = _state * 1664525 + 1013904223;
        
        // Trả về số dương (loại bỏ bit dấu)
        return (int)(_state >> 1);
    }

    // Hàm lấy số trong khoảng [min, max) (thay thế Range)
    public int Range(int min, int max)
    {
        if (min >= max) return min;
        // Dùng toán học thuần túy để chia lấy dư
        return min + (Next() % (max - min));
    }
}