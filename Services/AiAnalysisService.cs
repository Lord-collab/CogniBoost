using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public static class AiAnalysisService
{
    private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";
    private const string ApiKeyPref = "cb_openrouter_key";
    private const string DefaultApiKey = "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";

    private static readonly Lazy<HttpClient> LazyHttp = new(() =>
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    });

    private static HttpClient Http => LazyHttp.Value;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<string?> GetApiKeyAsync()
    {
        try
        {
            var stored = await SecureStorage.Default.GetAsync(ApiKeyPref);
            if (!string.IsNullOrWhiteSpace(stored)) return stored;
        }
        catch { }
        return DefaultApiKey;
    }

    public static async Task SetApiKeyAsync(string key)
    {
        try { await SecureStorage.Default.SetAsync(ApiKeyPref, key); }
        catch { }
    }

    public static bool HasApiKey => true;

    public static async Task<string?> AnalyzeProgressAsync()
    {
        var key = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(key)) return null;

        try
        {
            var profile = await AccountStore.GetProfileAsync();
            var overall = await ProgressStore.GetOverallScoreAsync();
            var gamesPlayed = await ProgressStore.GetGamesPlayedCountAsync();
            var streak = await StreakService.GetCurrentStreakAsync();
            var bestIq = await ProgressStore.GetBestIqAsync();
            var achievements = await AchievementsService.UnlockedCountAsync();

            var skills = new Dictionary<string, object>();
            foreach (var meta in BrainSkillInfo.All)
            {
                var score = await ProgressStore.GetSkillScoreAsync(meta.Skill);
                var history = await ProgressStore.GetGameHistoryAsync();
                var count = history.Count(r => r.Skill == meta.Skill);
                skills[meta.Title] = new { score, games_played = count };
            }

            var recentHistory = (await ProgressStore.GetGameHistoryAsync())
                .Take(10)
                .Select(r => new { r.GameTitle, r.AccuracyPercent })
                .ToList();

            var payload = new
            {
                username = profile?.Username ?? "Гость",
                overall_score = overall,
                rank = GetRank(overall),
                skills,
                total_games = gamesPlayed,
                streak,
                best_iq = bestIq,
                achievements_unlocked = achievements,
                recent_accuracy = recentHistory
            };

            var json = JsonSerializer.Serialize(payload, JsonOpts);

            var prompt = new
            {
                model = "deepseek/deepseek-chat",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Ты — AI-ассистент приложения CogniBoost для тренировки мозга. " +
                                  $"Проанализируй прогресс пользователя на русском языке. " +
                                  $"Напиши 3-4 предложения: какие навыки сильные, какие слабые, " +
                                  $"что стоит тренировать. Упомяни конкретные игры. " +
                                  $"Вот данные пользователя:\n\n{json}"
                    }
                },
                max_tokens = 500
            };

            var request = new HttpRequestMessage(HttpMethod.Post, OpenRouterEndpoint);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", key);
            request.Content = new StringContent(
                JsonSerializer.Serialize(prompt, JsonOpts),
                Encoding.UTF8,
                "application/json");

            var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content;
        }
        catch
        {
            return null;
        }
    }

    private static string GetRank(int score) => score switch
    {
        >= 900 => "Гений",
        >= 700 => "Мастер",
        >= 500 => "Эксперт",
        >= 300 => "Продвинутый",
        >= 150 => "Ученик",
        _ => "Новичок"
    };
}
