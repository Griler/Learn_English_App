using UnityEngine;
using System.Collections;
using Firebase.Database;
using Photon.Pun; // Giả sử bạn dùng SDK này

public class MatchmakingManager : MonoBehaviour
{
    public int maxWaitTime = 10; // 15 giây không tìm thấy ai thì gặp Bot
    private bool matchFound = false;
    private string currentRoomId;
    
    // Hàm bắt đầu tìm trận

    public void CreateFakeBotMatch(int rank )
    {
        Debug.Log("Hết giờ! Đang triệu hồi Bot...");

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        //PhotonNetwork.NickName = BotController.GetRandomBotName();
        props["AvatarID"] = "avatar_1";
        props["BorderID"] = "";
        int minRank = Mathf.Max(0, rank - 10);
        props["Rank"] = UnityEngine.Random.Range(minRank, rank + 10 + 1);   
        props["IsReady"] = true; // Mặc định vào phòng là chưa Ready
        props["IsBot"] = true; // Mặc định vào phòng là chưa Ready
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void BotJoinMatch(string matchId)
    {
        PhotonNetwork.JoinRoom(matchId);
    }
    // Hàm này được gọi từ Firebase Listener khi có người join
    public void OnPlayerJoined()
    {
        matchFound = true;
        // Logic vào game PvP thật
    }
}