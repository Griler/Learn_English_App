using System;
using System.Collections.Generic; // Cần cái này để dùng List

[Serializable]
public class UserInfoData
{
    public string avatar;
    public string border;
    public int coin;
    public string email;
    public string name;
    public int rankPoint;

    public UserInfoData(string avatar,string border,string email, string name, int coin, int rank)
    {
        this.avatar = avatar;
        this.border = border;
        this.email = email;
        this.coin = coin;
        this.name = name;
        this.rankPoint = rank;
    }
}

[Serializable]
public class FriendData
{
    public string userId;
    // Có thể thêm name, level của bạn bè ở đây nếu database có
}

// Class chứa dữ liệu tổng hợp để lưu trong SO
[Serializable]
public class UserFullData
{
    public UserInfoData userInfo;
    public List<FriendData> friendList = new List<FriendData>();
}