using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Система баллов (игровая валюта).
///
/// AwardForResult(accuracy): точность преобразуется в баллы по формуле
///   BasePoints (10) + нормализованная точность * MaxScaledPoints (40).
///   При >= 90% добавляется NearPerfectBonus (+15).
///   Итого диапазон: 10–65 баллов за игру.
///
/// Баланс и пожизненные баллы хранятся в UserEntity.PointsBalance/Lifetime.
/// </summary>
public static class PointsService
{
    private const int BasePoints = 10;
    private const int MaxScaledPoints = 40;
    private const int NearPerfectBonus = 15;

    public static async Task<int> GetBalanceAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        return user?.PointsBalance ?? 0;
    }

    public static async Task<int> GetLifetimeEarnedAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        return user?.PointsLifetime ?? 0;
    }

    public static int AwardForResult(double accuracy)
    {
        var normalized = Math.Clamp(accuracy, 0, 1);
        var earned = (int)Math.Round(BasePoints + normalized * MaxScaledPoints);
        if (normalized >= 0.9)
            earned += NearPerfectBonus;

        _ = UpdatePointsAsync(earned);
        return earned;
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;
        _ = UpdatePointsAsync(amount);
    }

    public static async Task<(bool Success, string Message)> TrySpendAsync(int amount)
    {
        if (amount <= 0)
            return (false, "Некорректная стоимость.");

        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        var balance = user?.PointsBalance ?? 0;

        if (balance < amount)
            return (false, $"Нужно {amount} очков. На балансе: {balance}.");

        user!.PointsBalance = balance - amount;
        await DatabaseService.Db.UpdateAsync(user);
        return (true, $"Списано {amount} очков.");
    }

    private static async Task UpdatePointsAsync(int amount)
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        if (user is null) return;

        user.PointsBalance += amount;
        user.PointsLifetime += amount;
        await DatabaseService.Db.UpdateAsync(user);
    }
}
