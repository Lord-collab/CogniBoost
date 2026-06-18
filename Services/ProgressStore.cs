using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Хранилище прогресса: история сыгранных игр и результатов тестов,
/// агрегированные баллы по навыкам. Данные сохраняются локально per-user.
/// </summary>
public static class ProgressStore
{
    private const string GameHistoryPrefix = "cb_game_history_";
    private const string TestHistoryPrefix = "cb_test_history_";

    private const int MaxHistoryItems = 200;

    // ---------- Игры ----------

    public static void AddGameResult(GameResult result)
    {
        var history = GetGameHistory();
        history.Insert(0, result);
        if (history.Count > MaxHistoryItems)
        {
            history = history.Take(MaxHistoryItems).ToList();
        }

        Preferences.Default.Set(GameHistoryKey(), JsonSerializer.Serialize(history));
    }

    public static List<GameResult> GetGameHistory()
    {
        var raw = Preferences.Default.Get(GameHistoryKey(), string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<GameResult>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<GameResult>>(raw) ?? new List<GameResult>();
        }
        catch
        {
            return new List<GameResult>();
        }
    }

    /// <summary>Лучший результат (по очкам) для конкретной игры.</summary>
    public static int GetBestScore(string gameId)
        => GetGameHistory()
            .Where(r => string.Equals(r.GameId, gameId, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Score)
            .DefaultIfEmpty(0)
            .Max();

    public static int GetGamesPlayedCount()
        => GetGameHistory()
            .Select(r => r.GameId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    /// <summary>
    /// Балл по навыку (0..1000): средняя точность лучших попыток по играм этого навыка.
    /// </summary>
    public static int GetSkillScore(BrainSkill skill)
    {
        var history = GetGameHistory().Where(r => r.Skill == skill).ToList();
        if (history.Count == 0)
        {
            return 0;
        }

        var bestByGame = history
            .GroupBy(r => r.GameId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Max(r => r.Accuracy));

        var avg = bestByGame.Average();
        return (int)Math.Round(avg * 1000);
    }

    /// <summary>Общий балл «мозга»: среднее по всем навыкам.</summary>
    public static int GetOverallScore()
    {
        var scores = BrainSkillInfo.All
            .Select(meta => GetSkillScore(meta.Skill))
            .Where(s => s > 0)
            .ToList();

        return scores.Count == 0 ? 0 : (int)Math.Round(scores.Average());
    }

    // ---------- Тесты ----------

    public static void AddTestResult(TestResult result)
    {
        var history = GetTestHistory();
        history.Insert(0, result);
        if (history.Count > MaxHistoryItems)
        {
            history = history.Take(MaxHistoryItems).ToList();
        }

        Preferences.Default.Set(TestHistoryKey(), JsonSerializer.Serialize(history));
    }

    public static List<TestResult> GetTestHistory()
    {
        var raw = Preferences.Default.Get(TestHistoryKey(), string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<TestResult>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<TestResult>>(raw) ?? new List<TestResult>();
        }
        catch
        {
            return new List<TestResult>();
        }
    }

    public static int GetBestIq()
        => GetTestHistory().Select(r => r.IqScore).DefaultIfEmpty(0).Max();

    private static string GameHistoryKey() => $"{GameHistoryPrefix}{UserKey()}";
    private static string TestHistoryKey() => $"{TestHistoryPrefix}{UserKey()}";

    private static string UserKey()
    {
        var key = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(key) ? "guest" : key;
    }
}
