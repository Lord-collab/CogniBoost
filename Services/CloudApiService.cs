using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Low-level REST client for Supabase.
/// Handles HTTP, serialization, DTOs — no sync orchestration logic.
/// </summary>
public static class CloudApiService
{
    public static bool IsEnabled => SupabaseConfig.IsConfigured;

    private static readonly Lazy<HttpClient> LazyHttp = new(() =>
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(SupabaseConfig.ProjectUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("apikey", SupabaseConfig.AnonKey);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", SupabaseConfig.AnonKey);
        return client;
    });

    private static HttpClient Http => LazyHttp.Value;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static HttpRequestMessage Post<T>(string path, T payload, bool preferMerge = false)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        if (preferMerge)
            req.Headers.Add("Prefer", "resolution=merge-duplicates,return=representation");

        return req;
    }

    // ── Player ────────────────────────────────────────────────────

    internal static async Task<PlayerFullRow?> FetchPlayerAsync(string usernameKey)
    {
        var url = $"/rest/v1/players?username=eq.{usernameKey}&select=*";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var body = await resp.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<PlayerFullRow>>(body, JsonOpts);
        return rows?.FirstOrDefault();
    }

    internal static async Task<(string? PlayerId, bool Success, string Message)> UpsertPlayerAsync(
        string usernameKey, string displayName, string avatarEmoji, int age)
    {
        var payload = new[]
        {
            new
            {
                username     = usernameKey,
                display_name = displayName,
                avatar_emoji = avatarEmoji,
                age          = age
            }
        };

        var req = Post(
            "/rest/v1/players?on_conflict=username&select=id",
            payload,
            preferMerge: true);

        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            return (null, false, $"Не удалось сохранить профиль: {resp.StatusCode}. {body}");

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var rows = await JsonSerializer.DeserializeAsync<List<PlayerRow>>(stream, JsonOpts)
                   ?? new List<PlayerRow>();

        var player = rows.FirstOrDefault();
        if (player is null)
            return (null, false, "Supabase не вернул id игрока.");

        return (player.id, true, "Профиль синхронизирован.");
    }

    // ── Scores ────────────────────────────────────────────────────

    internal static async Task<(bool Success, string Message)> UpsertScoresAsync(
        string playerId,
        int overall, int memory, int focus, int language, int logic,
        int pointsBalance, int pointsLifetime)
    {
        var payload = new[]
        {
            new
            {
                player_id       = playerId,
                overall,
                memory,
                focus,
                language,
                logic,
                points_balance  = pointsBalance,
                points_lifetime = pointsLifetime,
                updated_at      = DateTime.UtcNow
            }
        };

        var req = Post(
            "/rest/v1/player_scores?on_conflict=player_id",
            payload,
            preferMerge: true);

        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            return (false, $"Не удалось сохранить очки: {resp.StatusCode}. {body}");

        return (true, "Очки синхронизированы.");
    }

    internal static async Task<(bool Success, string Message)> UpsertGameScoresAsync(
        string playerId, List<GameResult> history)
    {
        if (history.Count == 0)
            return (true, "Нет игр для синхронизации.");

        var best = history
            .GroupBy(r => r.GameId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.Score).First())
            .ToList();

        var payload = best.Select(r => new
        {
            player_id      = playerId,
            game_id        = r.GameId,
            best_score     = r.Score,
            accuracy_pct   = r.AccuracyPercent,
            last_played_at = r.PlayedAtUtc
        }).ToArray();

        var req = Post(
            "/rest/v1/game_scores?on_conflict=player_id,game_id",
            payload,
            preferMerge: true);

        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            return (false, $"Не удалось сохранить результаты игр: {resp.StatusCode}. {body}");

        return (true, "Результаты игр синхронизированы.");
    }

    internal static async Task SaveTestResultAsync(string playerId, TestResult result)
    {
        var payload = new[]
        {
            new
            {
                player_id = playerId,
                test_id   = result.TestId,
                iq_score  = result.IqScore,
                correct   = result.CorrectAnswers,
                total     = result.TotalQuestions,
                played_at = result.PlayedAtUtc
            }
        };

        var req = Post("/rest/v1/test_results", payload);
        using var resp = await Http.SendAsync(req);
    }

    // ── Restore helpers ───────────────────────────────────────────

    internal static async Task<List<ScoresRow>> FetchScoresAsync(string playerId)
    {
        var url = $"/rest/v1/player_scores?player_id=eq.{playerId}&select=points_balance,points_lifetime";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return new();

        var body = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ScoresRow>>(body, JsonOpts) ?? new();
    }

    internal static async Task<List<GameScoreRow>> FetchGameHistoryAsync(string playerId)
    {
        var url = $"/rest/v1/game_scores?player_id=eq.{playerId}&select=*";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return new();

        var body = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GameScoreRow>>(body, JsonOpts) ?? new();
    }

    internal static async Task<List<TestResultRow>> FetchTestResultsAsync(string playerId)
    {
        var url = $"/rest/v1/test_results?player_id=eq.{playerId}&select=*&order=played_at.desc";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return new();

        var body = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TestResultRow>>(body, JsonOpts) ?? new();
    }

    // ── DTOs ──────────────────────────────────────────────────────

    internal sealed record PlayerRow(string id);

    internal sealed record PlayerFullRow(
        string id,
        string username,
        string? display_name,
        string? avatar_emoji,
        int age);

    internal sealed record ScoresRow(
        string player_id,
        int points_balance,
        int points_lifetime);

    internal sealed record GameScoreRow(
        string player_id,
        string game_id,
        int best_score,
        int accuracy_pct,
        string last_played_at);

    internal sealed record TestResultRow(
        string id,
        string player_id,
        string test_id,
        int iq_score,
        int correct,
        int total,
        string played_at);

    // ── Helpers ───────────────────────────────────────────────────

    internal static (string Title, BrainSkill Skill) GetGameMeta(string gameId)
    {
        return gameId.ToLowerInvariant() switch
        {
            "memory_pairs"    => ("Найди пары", BrainSkill.Memory),
            "color_sequence"  => ("Цветовая память", BrainSkill.Memory),
            "number_recall"   => ("Запомни число", BrainSkill.Memory),
            "reaction_tap"    => ("Быстрая реакция", BrainSkill.Focus),
            "spot_difference" => ("Найди изменение", BrainSkill.Focus),
            "stroop_color"    => ("Истинный цвет", BrainSkill.Focus),
            "number_series"   => ("Числовой ряд", BrainSkill.Logic),
            "matrix_logic"    => ("Матрица", BrainSkill.Logic),
            "odd_word"        => ("Лишнее слово", BrainSkill.Language),
            "word_chain"      => ("Цепочка слов", BrainSkill.Language),
            "simon_says"      => ("Повтори ряд", BrainSkill.Memory),
            "sudoku_mini"     => ("Судоку-мини", BrainSkill.Logic),
            "balance_scale"   => ("Весы", BrainSkill.Logic),
            "anagrams"        => ("Анаграммы", BrainSkill.Language),
            _                 => (gameId, BrainSkill.Logic),
        };
    }

    internal static string GetTestTitle(string testId)
    {
        return testId.ToLowerInvariant() switch
        {
            "iq_express"     => "Экспресс IQ-тест",
            "memory_words"   => "Тест памяти",
            "focus_test"     => "Тест внимания",
            "logic_test"     => "Тест на логику",
            "numerical_test" => "Числовой тест",
            _                => testId,
        };
    }

    internal static DateTime ParseUtc(string iso)
    {
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime();
        return DateTime.UtcNow;
    }
}
