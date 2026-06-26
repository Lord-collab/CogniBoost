using CogniBoost.Models;
using CogniBoost.Pages;
using CogniBoost.Services;

namespace CogniBoost.Services;

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
