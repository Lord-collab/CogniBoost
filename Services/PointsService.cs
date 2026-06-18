using CogniBoost.Services;

namespace CogniBoost.Services;

/// <summary>
/// Система бонусных очков: начисление за игры/тесты, баланс, трата на разблокировку.
/// Хранение — в Preferences, отдельно для каждого пользователя.
/// </summary>
public static class PointsService
{
    private const string BalancePrefix = "cb_points_balance_";
    private const string LifetimePrefix = "cb_points_lifetime_";

    private const int BasePoints = 10;
    private const int MaxScaledPoints = 40;
    private const int NearPerfectBonus = 15;

    public static int GetBalance() => Preferences.Default.Get(BalanceKey(), 0);

    public static int GetLifetimeEarned() => Preferences.Default.Get(LifetimeKey(), 0);

    /// <summary>
    /// Начисляет очки за результат игры/теста пропорционально точности.
    /// Возвращает количество начисленных очков.
    /// </summary>
    public static int AwardForResult(double accuracy)
    {
        var normalized = Math.Clamp(accuracy, 0, 1);
        var earned = (int)Math.Round(BasePoints + normalized * MaxScaledPoints);
        if (normalized >= 0.9)
        {
            earned += NearPerfectBonus;
        }

        AddPoints(earned);
        return earned;
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Preferences.Default.Set(BalanceKey(), GetBalance() + amount);
        Preferences.Default.Set(LifetimeKey(), GetLifetimeEarned() + amount);
    }

    public static bool TrySpend(int amount, out string message)
    {
        if (amount <= 0)
        {
            message = "Некорректная стоимость.";
            return false;
        }

        var balance = GetBalance();
        if (balance < amount)
        {
            message = $"Нужно {amount} очков. На балансе: {balance}.";
            return false;
        }

        Preferences.Default.Set(BalanceKey(), balance - amount);
        message = $"Списано {amount} очков.";
        return true;
    }

    private static string BalanceKey() => $"{BalancePrefix}{UserKey()}";
    private static string LifetimeKey() => $"{LifetimePrefix}{UserKey()}";

    private static string UserKey()
    {
        var key = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(key) ? "guest" : key;
    }
}
