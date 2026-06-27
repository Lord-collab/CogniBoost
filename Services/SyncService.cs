using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Orchestrates data sync between local storage and Supabase cloud.
/// Uses CloudApiService for the low-level HTTP calls.
/// </summary>
public static class SyncService
{
    public static bool IsEnabled => CloudApiService.IsEnabled;

    public static CloudSyncResult LastResult { get; private set; } =
        new(false, "Синхронизация ещё не выполнялась.");

    // ── Sync ──────────────────────────────────────────────────────

    public static async Task<CloudSyncResult> SyncCurrentUserAsync()
    {
        if (!IsEnabled)
            return Set(new CloudSyncResult(false, "Облако не настроено."));

        System.Diagnostics.Debug.WriteLine("[SyncService] Starting sync...");
        try
        {
            var usernameKey = AuthService.GetCurrentUsernameKey();
            var profile = await UserDataService.GetProfileAsync();
            if (profile is null)
                return Set(new CloudSyncResult(false, "Нет авторизованного пользователя."));

            // 1. Upsert profile → get player id
            var (playerId, success, message) = await CloudApiService.UpsertPlayerAsync(
                usernameKey, profile.Username, profile.AvatarEmoji, profile.Age);

            System.Diagnostics.Debug.WriteLine($"[SyncService] Profile result: success={success}, playerId={playerId}, msg={message}");
            if (!success || playerId is null)
                return Set(new CloudSyncResult(false, message));

            // 2. Upsert scores
            var scoresResult = await CloudApiService.UpsertScoresAsync(
                playerId,
                await ProgressStore.GetOverallScoreAsync(),
                await ProgressStore.GetSkillScoreAsync(BrainSkill.Memory),
                await ProgressStore.GetSkillScoreAsync(BrainSkill.Focus),
                await ProgressStore.GetSkillScoreAsync(BrainSkill.Language),
                await ProgressStore.GetSkillScoreAsync(BrainSkill.Logic),
                await PointsService.GetBalanceAsync(),
                await PointsService.GetLifetimeEarnedAsync());

            System.Diagnostics.Debug.WriteLine($"[SyncService] Scores result: {scoresResult.Message}");
            if (!scoresResult.Success)
                return Set(new CloudSyncResult(false, scoresResult.Message));

            // 3. Upsert game history
            var history = await ProgressStore.GetGameHistoryAsync();
            var gamesResult = await CloudApiService.UpsertGameScoresAsync(playerId, history);

            System.Diagnostics.Debug.WriteLine($"[SyncService] Games result: {gamesResult.Message}");
            if (!gamesResult.Success)
                return Set(new CloudSyncResult(false, gamesResult.Message));

            return Set(new CloudSyncResult(true, "Синхронизация с облаком выполнена."));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncService] Exception: {ex}");
            return Set(new CloudSyncResult(false, $"Ошибка синхронизации: {ex.Message}"));
        }
    }

    public static async Task SyncTestResultAsync(TestResult result)
    {
        if (!IsEnabled) return;

        try
        {
            var usernameKey = AuthService.GetCurrentUsernameKey();
            var profile = await UserDataService.GetProfileAsync();
            if (profile is null)
                return;

            var (playerId, _, _) = await CloudApiService.UpsertPlayerAsync(
                usernameKey, profile.Username, profile.AvatarEmoji, profile.Age);

            if (playerId is null) return;

            await CloudApiService.SaveTestResultAsync(playerId, result);
        }
        catch { /* offline — not critical */ }
    }

    // ── Restore ───────────────────────────────────────────────────

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

            // 1. Find player in Supabase
            var player = await CloudApiService.FetchPlayerAsync(usernameKey);
            if (player is null)
                return new CloudSyncResult(false,
                    "Пользователь не найден в облаке. Сначала зарегистрируйтесь на другом устройстве.");

            // 2. If local data exists — skip
            var existingGame = await DatabaseService.Db
                .Table<GameResultEntity>()
                .Where(g => g.UsernameKey == usernameKey)
                .FirstOrDefaultAsync();
            if (existingGame is not null)
                return new CloudSyncResult(true, "Локальные данные актуальны.");

            // 3. Create local account if it doesn't exist
            var (signInSuccess, _) = await AuthService.TrySignInAsync(username ?? string.Empty, password ?? string.Empty);
            if (!signInSuccess)
                await AuthService.SaveAccountAsync(
                    string.IsNullOrWhiteSpace(player.display_name) ? username ?? usernameKey : player.display_name,
                    player.age,
                    password ?? string.Empty);

            // 4. Restore profile
            await RestoreProfileAsync(player);

            // 5. Restore scores
            await RestoreScoresAsync(player.id, usernameKey);

            // 6. Restore game history
            await RestoreGameHistoryAsync(player.id, usernameKey);

            // 7. Restore test results
            await RestoreTestResultsAsync(player.id, usernameKey);

            return new CloudSyncResult(true, "Данные восстановлены из облака.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncService] Restore error: {ex}");
            return new CloudSyncResult(false, $"Ошибка восстановления: {ex.Message}");
        }
    }

    // ── Private restore methods ───────────────────────────────────

    private static async Task RestoreProfileAsync(CloudApiService.PlayerFullRow player)
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(player.username);
        if (user is null) return;

        if (!string.IsNullOrWhiteSpace(player.avatar_emoji))
            user.AvatarEmoji = player.avatar_emoji;

        await DatabaseService.Db.UpdateAsync(user);
    }

    private static async Task RestoreScoresAsync(string playerId, string usernameKey)
    {
        var rows = await CloudApiService.FetchScoresAsync(playerId);
        var row = rows.FirstOrDefault();
        if (row is null) return;

        var user = await DatabaseService.Db.FindAsync<UserEntity>(usernameKey);
        if (user is null) return;

        user.PointsBalance = row.points_balance;
        user.PointsLifetime = row.points_lifetime;
        await DatabaseService.Db.UpdateAsync(user);
    }

    private static async Task RestoreGameHistoryAsync(string playerId, string usernameKey)
    {
        var rows = await CloudApiService.FetchGameHistoryAsync(playerId);
        if (rows.Count == 0) return;

        foreach (var r in rows)
        {
            var gameMeta = CloudApiService.GetGameMeta(r.game_id);
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
                PlayedAtUtc = CloudApiService.ParseUtc(r.last_played_at)
            });
        }
    }

    private static async Task RestoreTestResultsAsync(string playerId, string usernameKey)
    {
        var rows = await CloudApiService.FetchTestResultsAsync(playerId);
        if (rows.Count == 0) return;

        foreach (var r in rows)
        {
            await DatabaseService.Db.InsertAsync(new TestResultEntity
            {
                UsernameKey = usernameKey,
                TestId = r.test_id,
                TestTitle = CloudApiService.GetTestTitle(r.test_id),
                CorrectAnswers = r.correct,
                TotalQuestions = r.total,
                IqScore = r.iq_score,
                EarnedPoints = 0,
                PlayedAtUtc = CloudApiService.ParseUtc(r.played_at)
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static CloudSyncResult Set(CloudSyncResult result)
    {
        LastResult = result;
        return result;
    }
}
