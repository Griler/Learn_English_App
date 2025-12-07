using UnityEngine;

public static class EloCalculator
{
    // K_FACTOR quyết định độ biến động điểm.
    // Để trung bình trận thắng được 16-20 điểm, ta đặt K khoảng 32-40.
    // Ở đây mình đặt 38 để khi 2 người ngang nhau đá, thắng được +19 điểm.
    private const float K_FACTOR = 38f;

    // Giới hạn điểm cộng/trừ tối đa theo yêu cầu của bạn
    private const int MAX_CHANGE = 24;

    public enum GameResult
    {
        Win = 1,
        Loss = 0,
        Draw = 2 // Xử lý riêng
    }

    /// <summary>
    /// Tính số điểm thay đổi cho người chơi
    /// </summary>
    /// <param name="myRating">Điểm hiện tại của mình</param>
    /// <param name="opponentRating">Điểm hiện tại của đối thủ</param>
    /// <param name="result">Kết quả (Thắng/Thua/Hòa)</param>
    /// <returns>Số điểm cần cộng (dương) hoặc trừ (âm)</returns>
    public static int CalculateRatingChange(int myRating, int opponentRating, GameResult result)
    {
        // 1. Nếu Hòa: Theo yêu cầu "như nhau" -> Không cộng trừ gì cả (0 điểm)
        if (result == GameResult.Draw)
        {
            return 0; 
        }

        // 2. Tính điểm kỳ vọng (Expected Score)
        // Công thức Elo chuẩn: E = 1 / (1 + 10 ^ ((RatingDoiThu - RatingMinh) / 400))
        // Nếu mình rank thấp hơn đối thủ, WinChance sẽ nhỏ (vd: 0.2)
        // Nếu mình rank cao hơn đối thủ, WinChance sẽ lớn (vd: 0.8)
        float scoreDiff = opponentRating - myRating;
        float expectedScore = 1.0f / (1.0f + Mathf.Pow(10.0f, scoreDiff / 400.0f));

        // 3. Tính điểm thực tế (Actual Score)
        float actualScore = (float)result; // Win = 1, Loss = 0

        // 4. Công thức Elo: Delta = K * (Thực tế - Kỳ vọng)
        float rawChange = K_FACTOR * (actualScore - expectedScore);

        // 5. Làm tròn số nguyên
        int change = Mathf.RoundToInt(rawChange);

        // 6. Kẹp giá trị (Clamp) để không vượt quá giới hạn -24 đến 24
        change = Mathf.Clamp(change, -MAX_CHANGE, MAX_CHANGE);

        return change;
    }
}