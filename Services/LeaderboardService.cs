using System.Net.Http.Headers;
using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Запись в таблице лидеров.
/// </summary>
public sealed record LeaderboardEntry(
    int Rank,
    string Name,
    int Score,
    bool IsCurrentPlayer,
    string AvatarEmoji);

public sealed record LeaderboardResult(
    IReadOnlyList<LeaderboardEntry> Entries,
    bool IsLive,
    string Message);

/// <summary>
/// Сервис таблицы лидеров.
/// При наличии Supabase-ключей загружает живой рейтинг;
/// при недоступности облака или отсутствии конфига — локальный фолбэк.
/// </summary>
public static class LeaderboardService
{
    private static readonly string[] SampleNames =
    {
        "Алекс", "Мария", "Иван", "София", "Дмитрий", "Анна",
        "Кирилл", "Лена", "Павел", "Ника", "Олег", "Вера",
        "Тимур", "Юля", "Глеб", "Дина"
    };

    private static readonly string[] SampleAvatars =
    {
        "\U0001F9E0", "\U0001F3AF", "\U0001F4AC", "\U0001F9E9",
        "\u26A1", "\U0001F680", "\u2B50", "\U0001F3B2"
    };

    // ----------------------------------------------------------------
    // HTTP (переиспользуем тот же подход, что в CloudSyncService)
    // ----------------------------------------------------------------
    private static readonly Lazy<HttpClient> LazyHttp = new(() =>
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(SupabaseConfig.ProjectUrl),
            Timeout = TimeSpan.FromSeconds(10)
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

    // DTO для десериализации строки leaderboard_overall
    private sealed record LeaderboardRow(
        string id,
        string username,
        string display_name,
        string avatar_emoji,
        int overall_score);

    // ----------------------------------------------------------------
    // Публичный метод
    // ----------------------------------------------------------------

    public static async Task<LeaderboardResult> GetLeaderboardAsync(int limit = 25)
    {
        // Сначала пытаемся синхронизировать текущего игрока
        if (SupabaseConfig.IsConfigured)
        {
            _ = CloudSyncService.SyncCurrentUserAsync(); // fire-and-forget
        }

        if (!SupabaseConfig.IsConfigured)
            return await BuildFallbackAsync("Облако не настроено. Показан локальный рейтинг.");

        try
        {
            var url = $"/rest/v1/leaderboard_overall" +
                      $"?select=id,username,display_name,avatar_emoji,overall_score" +
                      $"&order=overall_score.desc" +
                      $"&limit={limit}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await Http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[Leaderboard] HTTP {resp.StatusCode}: {err}");
                return await BuildFallbackAsync(
                    $"Не удалось загрузить рейтинг ({resp.StatusCode}): {err}");
            }

            await using var stream = await resp.Content.ReadAsStreamAsync();
            var rows = await JsonSerializer
                           .DeserializeAsync<List<LeaderboardRow>>(stream, JsonOpts)
                       ?? new List<LeaderboardRow>();

            if (rows.Count == 0)
                return await BuildFallbackAsync("В облаке пока нет данных. Сыграй несколько игр.");

            var currentKey = AccountStore.GetCurrentUsernameKey();
            var entries = rows
                .Select((row, idx) =>
                {
                    var isCurrent = string.Equals(
                        row.username, currentKey, StringComparison.OrdinalIgnoreCase);
                    var name = string.IsNullOrWhiteSpace(row.display_name)
                        ? row.username
                        : row.display_name;
                    var emoji = string.IsNullOrWhiteSpace(row.avatar_emoji)
                        ? AccountStore.DefaultAvatar
                        : row.avatar_emoji;
                    return new LeaderboardEntry(idx + 1, name, row.overall_score,
                        isCurrent, emoji);
                })
                .ToList();

            // Если текущего игрока нет в топ-N — добавляем его отдельно
            if (!string.IsNullOrWhiteSpace(currentKey) &&
                !entries.Any(e => e.IsCurrentPlayer))
            {
                var myScore = await ProgressStore.GetOverallScoreAsync();
                if (myScore > 0)
                {
                    entries.Add(new LeaderboardEntry(
                        entries.Count + 1,
                        await GetCurrentPlayerNameAsync(),
                        myScore,
                        true,
                        await GetCurrentPlayerAvatarAsync()));
                }
            }

            return new LeaderboardResult(entries, true,
                "Живой рейтинг из облака.");
        }
        catch (Exception ex)
        {
            return await BuildFallbackAsync($"Нет соединения с облаком. {ex.Message}");
        }
    }

    // ----------------------------------------------------------------
    // Локальный фолбэк
    // ----------------------------------------------------------------

    private static async Task<LeaderboardResult> BuildFallbackAsync(string message)
    {
        var playerScore = await ProgressStore.GetOverallScoreAsync();
        var playerName  = await GetCurrentPlayerNameAsync();
        var playerEmoji = await GetCurrentPlayerAvatarAsync();

        var seed = HashCode.Combine(playerName, playerScore);
        var rng  = new Random(seed);

        var entries = new List<LeaderboardEntry>();
        for (var i = 0; i < 12; i++)
        {
            var offset = 180 - i * 26 + rng.Next(-15, 16);
            var score  = Math.Clamp(playerScore + offset, 50, 1000);
            entries.Add(new LeaderboardEntry(
                Rank: 0,
                Name: SampleNames[i % SampleNames.Length],
                Score: score,
                IsCurrentPlayer: false,
                AvatarEmoji: SampleAvatars[i % SampleAvatars.Length]));
        }

        entries.Add(new LeaderboardEntry(0, playerName, playerScore, true, playerEmoji));

        var ranked = entries
            .OrderByDescending(e => e.Score)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select((e, idx) => e with { Rank = idx + 1 })
            .ToList();

        return new LeaderboardResult(ranked, false, message);
    }

    private static async Task<string> GetCurrentPlayerNameAsync()
    {
        var p = await AccountStore.GetProfileAsync();
        if (p is not null && !string.IsNullOrWhiteSpace(p.Username))
            return p.Username;
        return "Вы";
    }

    private static async Task<string> GetCurrentPlayerAvatarAsync()
    {
        var p = await AccountStore.GetProfileAsync();
        if (p is not null && !string.IsNullOrWhiteSpace(p.AvatarEmoji))
            return p.AvatarEmoji;
        return AccountStore.DefaultAvatar;
    }
}
