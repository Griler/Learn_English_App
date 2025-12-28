using UnityEngine;

public static class BotMatchHelper
{
    public static bool IsBotMatch = false; // Cờ đánh dấu trận đấu này là với Bot
    public static string BotName = "Bot";
    public static int BotRank = 0;
    public static string BotAvatarID = "1";
    public static string BotBorderID = "1";
    public static int BotAccuracy = 70; // Độ thông minh của Bot (0-100)

    public static void SetupBotMatch(int playerRank)
    {
        IsBotMatch = true;
        
        // 1. Random Tên
        string[] names = { "Alex", "David", "Sarah", "Emily", "Michael", "Kevin", "Jessica", "Daniel" };
        BotName = names[Random.Range(0, names.Length)];
        
        // 2. Random Rank (Quanh mức rank của người chơi)
        int minRank = Mathf.Max(0, playerRank - 100);
        BotRank = Random.Range(minRank, playerRank + 150);
        
        // 3. Random Avatar & Border
        BotAvatarID = "avatar_" + Random.Range(1, 5).ToString();  // Giả sử bạn có 5 khung
        
        // 4. Tính độ khôn
        BotAccuracy = 60 + (playerRank / 100); 
        if (BotAccuracy > 80) BotAccuracy = 80;
    }  
    public static void Reset()
    {
        IsBotMatch = false;
    }
}