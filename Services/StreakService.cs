using CogniBoost.Models;

namespace CogniBoost.Services;

public static class StreakService
{
    private const int BonusPerStreakDay = 10;
    private const int BonusThreshold = 3;

    public static async Task<int> GetCurrentStreakAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        return user?.StreakCurrent ?? 0;
    }

    public static async Task<int> GetLongestStreakAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        return user?.StreakLongest ?? 0;
    }

    public static async Task<bool> TrainedTodayAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        if (user?.StreakLastDate is null) return false;
        return string.Equals(user.StreakLastDate, TodayKey(), StringComparison.Ordinal);
    }

    public static async Task<(int Streak, int Bonus)> RecordActivityAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        if (user is null) return (0, 0);

        var today = TodayKey();
        var yesterday = YesterdayKey();
        var lastDay = user.StreakLastDate ?? string.Empty;

        if (string.Equals(lastDay, today, StringComparison.Ordinal))
            return (user.StreakCurrent, 0);

        int newStreak;
        if (string.Equals(lastDay, yesterday, StringComparison.Ordinal))
            newStreak = user.StreakCurrent + 1;
        else
            newStreak = 1;

        user.StreakLastDate = today;
        user.StreakCurrent = newStreak;

        if (newStreak > user.StreakLongest)
            user.StreakLongest = newStreak;

        await DatabaseService.Db.UpdateAsync(user);

        var bonus = newStreak >= BonusThreshold ? BonusPerStreakDay * (newStreak / BonusThreshold) : 0;
        if (bonus > 0)
            PointsService.AddPoints(bonus);

        return (newStreak, bonus);
    }

    private static string TodayKey() => DateTime.UtcNow.ToString("yyyyMMdd");
    private static string YesterdayKey() => DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd");
}
