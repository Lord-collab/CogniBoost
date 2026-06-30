using CogniBoost.Models;
using CogniBoost.Pages;
using CogniBoost.Services;

namespace CogniBoost.Services;

/// <summary>
/// Ежедневное задание: случайная (но детерминированная — от даты) игра
/// из числа открытых. За прохождение задания — удвоенные бонусы (×2).
/// </summary>
public static class DailyChallengeService
{
    public static GameDefinition? GetTodayChallenge()
    {
        var today = DateTime.UtcNow.Date;
        var seed = today.Year * 10000 + today.Month * 100 + today.Day;
        var rng = new Random(seed);
        var all = GameCatalog.All.Where(g => UnlockService.IsUnlocked(g)).ToList();
        if (all.Count == 0) return null;
        return all[rng.Next(all.Count)];
    }

    public static double BonusMultiplier => 2.0;

    public static bool IsChallengeGame(string gameId)
    {
        var challenge = GetTodayChallenge();
        return challenge?.Id == gameId;
    }
}
