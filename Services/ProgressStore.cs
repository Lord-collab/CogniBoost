using CogniBoost.Models;

namespace CogniBoost.Services;

public static class ProgressStore
{
    private const int MaxHistoryItems = 200;

    public static void AddGameResult(GameResult result)
    {
        DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
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
                UsernameKey = UserKey(),
                GameId = result.GameId,
                GameTitle = result.GameTitle,
                Skill = (int)result.Skill,
                Score = result.Score,
                MaxScore = result.MaxScore,
                EarnedPoints = result.EarnedPoints,
                PlayedAtUtc = result.PlayedAtUtc
            });
        });
    }

    public static List<GameResult> GetGameHistory()
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
            var entities = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == userKey)
                .OrderByDescending(g => g.PlayedAtUtc)
                .ToListAsync();

            return entities.Select(e => new GameResult(
                e.GameId, e.GameTitle, (BrainSkill)e.Skill,
                e.Score, e.MaxScore, e.EarnedPoints, e.PlayedAtUtc
            )).ToList();
        });
    }

    public static int GetBestScore(string gameId)
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
            var best = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == userKey
                    && g.GameId == gameId)
                .OrderByDescending(g => g.Score)
                .FirstOrDefaultAsync();

            return best?.Score ?? 0;
        });
    }

    public static int GetGamesPlayedCount()
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
            var entities = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == userKey)
                .ToListAsync();

            return entities
                .Select(g => g.GameId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        });
    }

    public static int GetSkillScore(BrainSkill skill)
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
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
        });
    }

    public static int GetOverallScore()
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
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
        });
    }

    // ---------- Тесты ----------

    public static void AddTestResult(TestResult result)
    {
        DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
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
                UsernameKey = UserKey(),
                TestId = result.TestId,
                TestTitle = result.TestTitle,
                CorrectAnswers = result.CorrectAnswers,
                TotalQuestions = result.TotalQuestions,
                IqScore = result.IqScore,
                EarnedPoints = result.EarnedPoints,
                PlayedAtUtc = result.PlayedAtUtc
            });
        });
    }

    public static List<TestResult> GetTestHistory()
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
            var entities = await DatabaseService.Db
                .Table<TestResultEntity>()
                .Where(t => t.UsernameKey == userKey)
                .OrderByDescending(t => t.PlayedAtUtc)
                .ToListAsync();

            return entities.Select(e => new TestResult(
                e.TestId, e.TestTitle, e.CorrectAnswers,
                e.TotalQuestions, e.IqScore, e.EarnedPoints, e.PlayedAtUtc
            )).ToList();
        });
    }

    public static int GetBestIq()
    {
        return DatabaseService.Sync(async () =>
        {
            var userKey = UserKey();
            var best = await DatabaseService.Db
                .Table<TestResultEntity>()
                .Where(t => t.UsernameKey == userKey)
                .OrderByDescending(t => t.IqScore)
                .FirstOrDefaultAsync();

            return best?.IqScore ?? 0;
        });
    }

    private static string UserKey()
    {
        var key = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(key) ? "guest" : key;
    }
}
