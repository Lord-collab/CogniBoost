using CogniBoost.Models;

namespace CogniBoost.Services;

public static class PointsService
{
    private const int BasePoints = 10;
    private const int MaxScaledPoints = 40;
    private const int NearPerfectBonus = 15;

    public static int GetBalance()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            return user?.PointsBalance ?? 0;
        });
    }

    public static int GetLifetimeEarned()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            return user?.PointsLifetime ?? 0;
        });
    }

    public static int AwardForResult(double accuracy)
    {
        var normalized = Math.Clamp(accuracy, 0, 1);
        var earned = (int)Math.Round(BasePoints + normalized * MaxScaledPoints);
        if (normalized >= 0.9)
            earned += NearPerfectBonus;

        AddPoints(earned);
        return earned;
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;
        UpdatePoints(amount);
    }

    public static bool TrySpend(int amount, out string message)
    {
        if (amount <= 0)
        {
            message = "Некорректная стоимость.";
            return false;
        }

        var result = DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            var balance = user?.PointsBalance ?? 0;

            if (balance < amount)
                return (Success: false, Message: $"Нужно {amount} очков. На балансе: {balance}.");

            user!.PointsBalance = balance - amount;
            await DatabaseService.Db.UpdateAsync(user);
            return (Success: true, Message: $"Списано {amount} очков.");
        });

        message = result.Message;
        return result.Success;
    }

    private static void UpdatePoints(int amount)
    {
        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            if (user is null) return;

            user.PointsBalance += amount;
            user.PointsLifetime += amount;
            await DatabaseService.Db.UpdateAsync(user);
        });
    }

    private static string UserKey()
        => AccountStore.GetCurrentUsernameKey();
}
