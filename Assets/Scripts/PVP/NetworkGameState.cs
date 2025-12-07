// NetworkGameState.cs
public static class NetworkGameState
{
    public enum JoinType
    {
        None,
        RandomMatchmaking, // Tìm trận ngẫu nhiên
        FriendInvite       // Vào phòng bạn bè
    }

    // Biến này sẽ cho biết ai là người vừa ra lệnh kết nối
    public static JoinType CurrentJoinType = JoinType.None;
}