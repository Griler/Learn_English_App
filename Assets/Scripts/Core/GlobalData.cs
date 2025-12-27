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

    public static Dictionary<string, string> mapNameVocabulary = new Dictionary<string, string>
    {
        // --- 1. TOPIC CHÍNH (Root Keys) ---
        { "animals", "Động Vật" },
        { "fruits", "Trái Cây" },
        { "school", "Trường Học" },
        { "vehicles", "Phương Tiện" },

        // --- 2. TOPIC CON (Sub Keys - Animals) ---
        { "Farm Animal", "Động Vật Nông Trại" },
        { "Pet", "Thú Cưng" },
        { "Sea Animal", "Động Vật Biển" },
        { "Wild Animal", "Động Vật Hoang Dã" },

        // --- 3. TOPIC CON (Sub Keys - Fruits) ---
        { "Berry Fruits", "Các Loại Quả Mọng" },
        { "Citrus Fruits", "Họ Cam Quýt" },
        { "Tropical Fruits", "Trái Cây Nhiệt Đới" },

        // --- 4. TOPIC CON (Sub Keys - School) ---
        { "Classroom Items", "Dụng Cụ Lớp Học" },
        { "People at School", "Mọi Người Ở Trường" },
        { "Stationery", "Văn Phòng Phẩm" },

        // --- 5. TOPIC CON (Sub Keys - Vehicles) ---
        { "Air Vehicles", "Phương Tiện Hàng Không" },
        { "Land Vehicles", "Phương Tiện Đường Bộ" },
        { "Water Vehicles", "Phương Tiện Đường Thủy" }
    };
    
    public static Dictionary<string, string> mapNameGrammar = new Dictionary<string, string>()
    {
        // --- HIỆN TẠI (PRESENT) ---
        { "PRESENT_SIMPLE", "Thì hiện tại đơn" },
        { "PRESENT_CONTINUOUS", "Thì hiện tại tiếp diễn" },
        { "PRESENT_PERFECT", "Thì hiện tại hoàn thành" },
        { "PRESENT_PERFECT_CONTINUOUS", "Thì hiện tại hoàn thành tiếp diễn" },

        // --- QUÁ KHỨ (PAST) ---
        { "PAST_SIMPLE", "Thì quá khứ đơn" },
        { "PAST_CONTINUOUS", "Thì quá khứ tiếp diễn" },
        { "PAST_PERFECT", "Thì quá khứ hoàn thành" },
        { "PAST_PERFECT_CONTINUOUS", "Thì quá khứ hoàn thành tiếp diễn" },

        // --- TƯƠNG LAI (FUTURE) ---
        { "FUTURE_SIMPLE", "Thì tương lai đơn" },
        { "FUTURE_CONTINUOUS", "Thì tương lai tiếp diễn" },
        { "FUTURE_PERFECT", "Thì tương lai hoàn thành" },
        { "FUTURE_PERFECT_CONTINUOUS", "Thì tương lai hoàn thành tiếp diễn" }
    };
    
    public static List<string> sortOrder = new List<string>()
    {
        "PRESENT_SIMPLE",
        "PAST_SIMPLE",
        "FUTURE_SIMPLE",
        "PRESENT_CONTINUOUS",
        "PAST_CONTINUOUS",
        "FUTURE_CONTINUOUS",
        "PRESENT_PERFECT",
        "PAST_PERFECT",
        "FUTURE_PERFECT",
        "PRESENT_PERFECT_CONTINUOUS",
        "PAST_PERFECT_CONTINUOUS",
        "FUTURE_PERFECT_CONTINUOUS"
    };
    
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

public static class GameSessionData
{
    // Lưu danh sách các SubTopic của chủ đề hiện tại (ví dụ: ["Farm Animal", "Pet", "Wild Animal"...])
    public static List<string> CurrentSubTopics = new List<string>();
}