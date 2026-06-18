namespace CogniBoost.Services;

/// <summary>
/// Отслеживает серию последовательных дней тренировок.
/// Streak засчитывается при первом завершении игры/теста за день.
/// </summary>
public static class StreakService
{
    private const string LastDayPrefix    = "cb_streak_last_";
    private const string CurrentPrefix    = "cb_streak_current_";
    private const string LongestPrefix    = "cb_streak_longest_";

    private const int BonusPerStreakDay   = 10;
    private const int BonusThreshold      = 3;   // начиная с 3 дней даём бонус

    public static int GetCurrentStreak()
        => Preferences.Default.Get(CurrentKey(), 0);

    public static int GetLongestStreak()
        => Preferences.Default.Get(LongestKey(), 0);

    public static bool TrainedToday()
    {
        var last = Preferences.Default.Get(LastDayKey(), string.Empty);
        return string.Equals(last, TodayKey(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Вызывается при завершении игры или теста.
    /// Возвращает (серия, бонусные очки) — бонус > 0 если streak продолжен.
    /// </summary>
    public static (int Streak, int Bonus) RecordActivity()
    {
        var today     = TodayKey();
        var yesterday = YesterdayKey();
        var lastDay   = Preferences.Default.Get(LastDayKey(), string.Empty);

        // Уже тренировались сегодня — ничего не меняем
        if (string.Equals(lastDay, today, StringComparison.Ordinal))
            return (GetCurrentStreak(), 0);

        int newStreak;
        if (string.Equals(lastDay, yesterday, StringComparison.Ordinal))
        {
            // Вчера тренировались — продолжаем серию
            newStreak = GetCurrentStreak() + 1;
        }
        else
        {
            // Пропустили — начинаем заново
            newStreak = 1;
        }

        Preferences.Default.Set(LastDayKey(), today);
        Preferences.Default.Set(CurrentKey(), newStreak);

        var longest = GetLongestStreak();
        if (newStreak > longest)
            Preferences.Default.Set(LongestKey(), newStreak);

        // Бонус за streak
        var bonus = newStreak >= BonusThreshold ? BonusPerStreakDay * (newStreak / BonusThreshold) : 0;
        if (bonus > 0)
            PointsService.AddPoints(bonus);

        return (newStreak, bonus);
    }

    private static string TodayKey()
        => DateTime.UtcNow.ToString("yyyyMMdd");

    private static string YesterdayKey()
        => DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd");

    private static string LastDayKey()    => $"{LastDayPrefix}{UserKey()}";
    private static string CurrentKey()   => $"{CurrentPrefix}{UserKey()}";
    private static string LongestKey()   => $"{LongestPrefix}{UserKey()}";

    private static string UserKey()
    {
        var k = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(k) ? "guest" : k;
    }
}
