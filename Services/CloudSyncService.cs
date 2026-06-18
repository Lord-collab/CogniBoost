using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public sealed record CloudSyncResult(bool IsSuccess, string Message);

/// <summary>
/// Синхронизация данных игрока с Supabase через REST API.
/// Все запросы идут на /rest/v1/ с anon-ключом.
/// При недоступности облака приложение продолжает работать офлайн.
/// </summary>
public static class CloudSyncService
{
    public static bool IsEnabled => SupabaseConfig.IsConfigured;

    public static CloudSyncResult LastResult { get; private set; } =
        new(false, "Синхронизация ещё не выполнялась.");

    // ----------------------------------------------------------------
    // HTTP-клиент (один экземпляр на весь жизненный цикл)
    // ----------------------------------------------------------------
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

    // ----------------------------------------------------------------
    // Публичный API
    // ----------------------------------------------------------------

    /// <summary>
    /// Полная синхронизация: профиль + очки + все сыгранные игры.
    /// Вызывается в фоне после каждой завершённой игры/теста.
    /// </summary>
    public static async Task<CloudSyncResult> SyncCurrentUserAsync()
    {
        if (!IsEnabled)
            return Set(new CloudSyncResult(false, "Облако не настроено."));

        System.Diagnostics.Debug.WriteLine("[CloudSync] Starting sync...");
        try
        {
            // 1. Upsert профиля → получаем id игрока
            var (playerId, profileResult) = await EnsurePlayerAsync();
            System.Diagnostics.Debug.WriteLine($"[CloudSync] Profile result: {profileResult.IsSuccess}, playerId={playerId}, msg={profileResult.Message}");
            if (!profileResult.IsSuccess || playerId is null)
                return Set(profileResult);

            // 2. Upsert очков по навыкам
            var scoresResult = await UpsertScoresAsync(playerId);
            System.Diagnostics.Debug.WriteLine($"[CloudSync] Scores result: {scoresResult.IsSuccess}, msg={scoresResult.Message}");
            if (!scoresResult.IsSuccess)
                return Set(scoresResult);

            // 3. Upsert результатов игр
            var gamesResult = await UpsertGameScoresAsync(playerId);
            System.Diagnostics.Debug.WriteLine($"[CloudSync] Games result: {gamesResult.IsSuccess}, msg={gamesResult.Message}");
            if (!gamesResult.IsSuccess)
                return Set(gamesResult);

            return Set(new CloudSyncResult(true, "Синхронизация с облаком выполнена."));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CloudSync] Exception: {ex}");
            return Set(new CloudSyncResult(false, $"Ошибка синхронизации: {ex.Message}"));
        }
    }

    /// <summary>Сохранить результат теста в облако.</summary>
    public static async Task SyncTestResultAsync(TestResult result)
    {
        if (!IsEnabled) return;

        try
        {
            var (playerId, _) = await EnsurePlayerAsync();
            if (playerId is null) return;

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
            // не критично — просто игнорируем ошибку
        }
        catch { /* офлайн — не страшно */ }
    }

    // ----------------------------------------------------------------
    // Внутренние методы
    // ----------------------------------------------------------------

    private static async Task<(string? PlayerId, CloudSyncResult Result)> EnsurePlayerAsync()
    {
        if (!AccountStore.TryGetCurrentProfile(out var profile))
            return (null, new CloudSyncResult(false, "Нет авторизованного пользователя."));

        var usernameKey = AccountStore.GetCurrentUsernameKey();
        System.Diagnostics.Debug.WriteLine($"[CloudSync] EnsurePlayerAsync: usernameKey={usernameKey}, profile.Username={profile.Username}");

        var payload = new[]
        {
            new
            {
                username     = usernameKey,
                display_name = profile.Username,
                avatar_emoji = profile.AvatarEmoji,
                age          = profile.Age
            }
        };

        var req = Post(
            "/rest/v1/players?on_conflict=username&select=id",
            payload,
            preferMerge: true);

        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"[CloudSync] EnsurePlayerAsync response: {resp.StatusCode} {body}");
        if (!resp.IsSuccessStatusCode)
        {
            return (null, new CloudSyncResult(false,
                $"Не удалось сохранить профиль: {resp.StatusCode}. {body}"));
        }

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var rows = await JsonSerializer.DeserializeAsync<List<PlayerRow>>(stream, JsonOpts)
                   ?? new List<PlayerRow>();

        var player = rows.FirstOrDefault();
        if (player is null)
            return (null, new CloudSyncResult(false, "Supabase не вернул id игрока."));

        System.Diagnostics.Debug.WriteLine($"[CloudSync] Got playerId: {player.id}");
        return (player.id, new CloudSyncResult(true, "Профиль синхронизирован."));
    }

    private static async Task<CloudSyncResult> UpsertScoresAsync(string playerId)
    {
        var payload = new[]
        {
            new
            {
                player_id       = playerId,
                overall         = ProgressStore.GetOverallScore(),
                memory          = ProgressStore.GetSkillScore(BrainSkill.Memory),
                focus           = ProgressStore.GetSkillScore(BrainSkill.Focus),
                language        = ProgressStore.GetSkillScore(BrainSkill.Language),
                logic           = ProgressStore.GetSkillScore(BrainSkill.Logic),
                points_balance  = PointsService.GetBalance(),
                points_lifetime = PointsService.GetLifetimeEarned(),
                updated_at      = DateTime.UtcNow
            }
        };

        System.Diagnostics.Debug.WriteLine($"[CloudSync] UpsertScores payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
        var req = Post(
            "/rest/v1/player_scores?on_conflict=player_id",
            payload,
            preferMerge: true);

        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"[CloudSync] UpsertScores response: {resp.StatusCode} {body}");
        if (!resp.IsSuccessStatusCode)
        {
            return new CloudSyncResult(false,
                $"Не удалось сохранить очки: {resp.StatusCode}. {body}");
        }

        return new CloudSyncResult(true, "Очки синхронизированы.");
    }

    private static async Task<CloudSyncResult> UpsertGameScoresAsync(string playerId)
    {
        var history = ProgressStore.GetGameHistory();
        if (history.Count == 0)
            return new CloudSyncResult(true, "Нет игр для синхронизации.");

        // Группируем по game_id — берём лучший результат
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

        System.Diagnostics.Debug.WriteLine($"[CloudSync] UpsertGameScores payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
        var req = Post(
            "/rest/v1/game_scores?on_conflict=player_id,game_id",
            payload,
            preferMerge: true);

        using var resp = await Http.SendAsync(req);
        var body2 = await resp.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"[CloudSync] UpsertGameScores response: {resp.StatusCode} {body2}");
        if (!resp.IsSuccessStatusCode)
        {
            return new CloudSyncResult(false,
                $"Не удалось сохранить результаты игр: {resp.StatusCode}. {body2}");
        }

        return new CloudSyncResult(true, "Результаты игр синхронизированы.");
    }

    // ----------------------------------------------------------------
    // Вспомогательные методы
    // ----------------------------------------------------------------

    private static HttpRequestMessage Post<T>(string path, T payload, bool preferMerge = false)
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

    private static CloudSyncResult Set(CloudSyncResult result)
    {
        LastResult = result;
        return result;
    }

    // DTO для десериализации ответа
    private sealed record PlayerRow(string id);
}
