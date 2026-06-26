using System.Globalization;
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

    // ----------------------------------------------------------------
    // Восстановление данных из облака при входе на новом устройстве
    // ----------------------------------------------------------------

    /// <summary>
    /// Найти игрока в Supabase по username и восстановить все данные
    /// в локальный Preferences (профиль, очки, игры, тесты).
    /// Если локальные данные уже есть — ничего не перезаписываем.
    /// </summary>
    public static async Task<CloudSyncResult> RestorePlayerDataAsync(
        string username, string password)
    {
        if (!IsEnabled)
            return new CloudSyncResult(false, "Облако не настроено.");

        try
        {
            var usernameKey = (username ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(usernameKey))
                return new CloudSyncResult(false, "Введите имя пользователя.");

            // 1. Ищем игрока в Supabase
            var player = await FetchPlayerAsync(usernameKey);
            if (player is null)
                return new CloudSyncResult(false,
                    "Пользователь не найден в облаке. Сначала зарегистрируйтесь на другом устройстве.");

            // 2. Если локально уже есть данные — не трогаем
            var existingGame = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == usernameKey)
                .FirstOrDefaultAsync();
            if (existingGame is not null)
                return new CloudSyncResult(true, "Локальные данные актуальны.");

            // 3. Создаём локальный аккаунт (если его нет)
            if (!AccountStore.TrySignIn(username ?? string.Empty, password ?? string.Empty, out _))
                AccountStore.SaveAccount(
                    string.IsNullOrWhiteSpace(player.display_name) ? username ?? usernameKey : player.display_name,
                    player.age,
                    password ?? string.Empty);

            // 4. Восстанавливаем профиль
            await RestoreProfileAsync(player);

            // 5. Восстанавливаем очки
            await RestoreScoresAsync(player.id, usernameKey);

            // 6. Восстанавливаем историю игр
            await RestoreGameHistoryAsync(player.id, usernameKey);

            // 7. Восстанавливаем результаты тестов
            await RestoreTestResultsAsync(player.id, usernameKey);

            return new CloudSyncResult(true, "Данные восстановлены из облака.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CloudSync] Restore error: {ex}");
            return new CloudSyncResult(false, $"Ошибка восстановления: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------
    // DTO для загрузки данных из Supabase
    // ----------------------------------------------------------------

    private sealed record PlayerRow(string id);

    private sealed record PlayerFullRow(
        string id,
        string username,
        string? display_name,
        string? avatar_emoji,
        int age);

    private sealed record ScoresRow(
        string player_id,
        int points_balance,
        int points_lifetime);

    private sealed record GameScoreRow(
        string player_id,
        string game_id,
        int best_score,
        int accuracy_pct,
        string last_played_at);

    private sealed record TestResultRow(
        string id,
        string player_id,
        string test_id,
        int iq_score,
        int correct,
        int total,
        string played_at);

    // ----------------------------------------------------------------
    // Приватные методы восстановления
    // ----------------------------------------------------------------

    private static async Task<PlayerFullRow?> FetchPlayerAsync(string usernameKey)
    {
        var url = $"/rest/v1/players?username=eq.{usernameKey}&select=*";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var body = await resp.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<PlayerFullRow>>(body, JsonOpts);
        return rows?.FirstOrDefault();
    }

    private static async Task RestoreProfileAsync(PlayerFullRow player)
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(player.username);
        if (user is null) return;

        if (!string.IsNullOrWhiteSpace(player.avatar_emoji))
            user.AvatarEmoji = player.avatar_emoji;

        await DatabaseService.Db.UpdateAsync(user);
    }

    private static async Task RestoreScoresAsync(string playerId, string usernameKey)
    {
        var url = $"/rest/v1/player_scores?player_id=eq.{playerId}&select=points_balance,points_lifetime";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<ScoresRow>>(body, JsonOpts);
        var row = rows?.FirstOrDefault();
        if (row is null) return;

        var user = await DatabaseService.Db.FindAsync<UserEntity>(usernameKey);
        if (user is null) return;

        user.PointsBalance = row.points_balance;
        user.PointsLifetime = row.points_lifetime;
        await DatabaseService.Db.UpdateAsync(user);
    }

    private static async Task RestoreGameHistoryAsync(string playerId, string usernameKey)
    {
        var url = $"/rest/v1/game_scores?player_id=eq.{playerId}&select=*";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<GameScoreRow>>(body, JsonOpts);
        if (rows is null || rows.Count == 0) return;

        foreach (var r in rows)
        {
            var gameMeta = GetGameMeta(r.game_id);
            var accuracy = r.accuracy_pct / 100.0;
            var maxScore = accuracy > 0
                ? (int)Math.Round(r.best_score / accuracy)
                : r.best_score;

            await DatabaseService.Db.InsertAsync(new GameResultEntity
            {
                UsernameKey = usernameKey,
                GameId = r.game_id,
                GameTitle = gameMeta.Title,
                Skill = (int)gameMeta.Skill,
                Score = r.best_score,
                MaxScore = maxScore,
                EarnedPoints = 0,
                PlayedAtUtc = ParseUtc(r.last_played_at)
            });
        }
    }

    private static async Task RestoreTestResultsAsync(string playerId, string usernameKey)
    {
        var url = $"/rest/v1/test_results?player_id=eq.{playerId}&select=*&order=played_at.desc";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<TestResultRow>>(body, JsonOpts);
        if (rows is null || rows.Count == 0) return;

        foreach (var r in rows)
        {
            await DatabaseService.Db.InsertAsync(new TestResultEntity
            {
                UsernameKey = usernameKey,
                TestId = r.test_id,
                TestTitle = GetTestTitle(r.test_id),
                CorrectAnswers = r.correct,
                TotalQuestions = r.total,
                IqScore = r.iq_score,
                EarnedPoints = 0,
                PlayedAtUtc = ParseUtc(r.played_at)
            });
        }
    }

    // ----------------------------------------------------------------
    // Вспомогательные методы
    // ----------------------------------------------------------------

    private static (string Title, BrainSkill Skill) GetGameMeta(string gameId)
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

    private static string GetTestTitle(string testId)
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

    private static DateTime ParseUtc(string iso)
    {
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime();
        return DateTime.UtcNow;
    }

}
