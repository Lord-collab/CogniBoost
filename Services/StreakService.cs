using CogniBoost.Models;

namespace CogniBoost.Services;

public static class StreakService
{
    private const int BonusPerStreakDay = 10;
    private const int BonusThreshold = 3;

    public static int GetCurrentStreak()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            return user?.StreakCurrent ?? 0;
        });
    }

    public static int GetLongestStreak()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            return user?.StreakLongest ?? 0;
        });
    }

    public static bool TrainedToday()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            if (user?.StreakLastDate is null) return false;
            return string.Equals(user.StreakLastDate, TodayKey(), StringComparison.Ordinal);
        });
    }

    public static (int Streak, int Bonus) RecordActivity()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
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
        });
    }

    private static string TodayKey() => DateTime.UtcNow.ToString("yyyyMMdd");
    private static string YesterdayKey() => DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd");

    private static string UserKey()
        => AccountStore.GetCurrentUsernameKey();
}
