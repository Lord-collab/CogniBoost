using CogniBoost.Models;

namespace CogniBoost.Services;

public static class ProgressStore
{
    private const int MaxHistoryItems = 200;

    public static async Task AddGameResultAsync(GameResult result)
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var count = await DatabaseService.Db
            .Table<GameResultEntity>()
            .Where(g => g.UsernameKey == userKey)
            .CountAsync();

        if (count >= MaxHistoryItems)
        {
            var oldest = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == userKey)
                .OrderBy(g => g.PlayedAtUtc)
                .FirstAsync();

            await DatabaseService.Db.DeleteAsync(oldest);
        }

        await DatabaseService.Db.InsertAsync(new GameResultEntity
        {
            UsernameKey = AccountStore.GetCurrentUsernameKey(),
            GameId = result.GameId,
            GameTitle = result.GameTitle,
            Skill = (int)result.Skill,
            Score = result.Score,
            MaxScore = result.MaxScore,
            EarnedPoints = result.EarnedPoints,
            PlayedAtUtc = result.PlayedAtUtc
        });
    }

    public static async Task<List<GameResult>> GetGameHistoryAsync()
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var entities = await DatabaseService.Db
            .Table<GameResultEntity>()
            .Where(g => g.UsernameKey == userKey)
            .OrderByDescending(g => g.PlayedAtUtc)
            .ToListAsync();

        return entities.Select(e => new GameResult(
            e.GameId, e.GameTitle, (BrainSkill)e.Skill,
            e.Score, e.MaxScore, e.EarnedPoints, e.PlayedAtUtc
        )).ToList();
    }

    public static async Task<int> GetBestScoreAsync(string gameId)
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var best = await DatabaseService.Db
            .Table<GameResultEntity>()
            .Where(g => g.UsernameKey == userKey
                && g.GameId == gameId)
            .OrderByDescending(g => g.Score)
            .FirstOrDefaultAsync();

        return best?.Score ?? 0;
    }

    public static async Task<int> GetGamesPlayedCountAsync()
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var entities = await DatabaseService.Db
            .Table<GameResultEntity>()
            .Where(g => g.UsernameKey == userKey)
            .ToListAsync();

        return entities
            .Select(g => g.GameId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    public static async Task<int> GetSkillScoreAsync(BrainSkill skill)
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var history = await DatabaseService.Db
            .Table<GameResultEntity>()
            .Where(g => g.UsernameKey == userKey
                && g.Skill == (int)skill)
            .ToListAsync();

        if (history.Count == 0) return 0;

        var bestByGame = history
            .GroupBy(r => r.GameId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var max = g.OrderByDescending(r => r.Score / (double)Math.Max(r.MaxScore, 1)).First();
                return max.MaxScore > 0
                    ? Math.Clamp(max.Score / (double)max.MaxScore, 0, 1)
                    : 0;
            });

        var avg = bestByGame.Average();
        return (int)Math.Round(avg * 1000);
    }

    public static async Task<int> GetOverallScoreAsync()
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var scores = new List<int>();
        foreach (var meta in BrainSkillInfo.All)
        {
            var history = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == userKey
                    && g.Skill == (int)meta.Skill)
                .ToListAsync();

            if (history.Count == 0) continue;

            var bestByGame = history
                .GroupBy(r => r.GameId, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var max = g.OrderByDescending(r => r.Score / (double)Math.Max(r.MaxScore, 1)).First();
                    return max.MaxScore > 0
                        ? Math.Clamp(max.Score / (double)max.MaxScore, 0, 1)
                        : 0;
                });

            var avg = bestByGame.Average();
            scores.Add((int)Math.Round(avg * 1000));
        }

        return scores.Count == 0 ? 0 : (int)Math.Round(scores.Average());
    }

    // ---------- Tests ----------

    public static async Task AddTestResultAsync(TestResult result)
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var count = await DatabaseService.Db
            .Table<TestResultEntity>()
            .Where(t => t.UsernameKey == userKey)
            .CountAsync();

        if (count >= MaxHistoryItems)
        {
            var oldest = await DatabaseService.Db
                .Table<TestResultEntity>()
                .Where(t => t.UsernameKey == userKey)
                .OrderBy(t => t.PlayedAtUtc)
                .FirstAsync();

            await DatabaseService.Db.DeleteAsync(oldest);
        }

        await DatabaseService.Db.InsertAsync(new TestResultEntity
        {
            UsernameKey = AccountStore.GetCurrentUsernameKey(),
            TestId = result.TestId,
            TestTitle = result.TestTitle,
            CorrectAnswers = result.CorrectAnswers,
            TotalQuestions = result.TotalQuestions,
            IqScore = result.IqScore,
            EarnedPoints = result.EarnedPoints,
            PlayedAtUtc = result.PlayedAtUtc
        });
    }

    public static async Task<List<TestResult>> GetTestHistoryAsync()
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var entities = await DatabaseService.Db
            .Table<TestResultEntity>()
            .Where(t => t.UsernameKey == userKey)
            .OrderByDescending(t => t.PlayedAtUtc)
            .ToListAsync();

        return entities.Select(e => new TestResult(
            e.TestId, e.TestTitle, e.CorrectAnswers,
            e.TotalQuestions, e.IqScore, e.EarnedPoints, e.PlayedAtUtc
        )).ToList();
    }

    public static async Task<int> GetBestIqAsync()
    {
        var userKey = AccountStore.GetCurrentUsernameKey();
        var best = await DatabaseService.Db
            .Table<TestResultEntity>()
            .Where(t => t.UsernameKey == userKey)
            .OrderByDescending(t => t.IqScore)
            .FirstOrDefaultAsync();

        return best?.IqScore ?? 0;
    }
}
