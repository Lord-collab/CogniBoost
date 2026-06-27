using System.Text.Json;
using CogniBoost.Models;
using SQLite;

namespace CogniBoost.Services;

public static class DatabaseService
{
    private static SQLiteAsyncConnection? _db;
    private static bool _initialized;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    public static SQLiteAsyncConnection Db =>
        _db ?? throw new InvalidOperationException(
            "DatabaseService не инициализирован.");

    public static async Task InitAsync()
    {
        if (_initialized) return;
        await InitLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var path = Path.Combine(FileSystem.AppDataDirectory, "cogniboost.db");
            _db = new SQLiteAsyncConnection(path,
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.SharedCache);

            await _db.CreateTableAsync<UserEntity>();
            await _db.CreateTableAsync<GameResultEntity>();
            await _db.CreateTableAsync<TestResultEntity>();
            await _db.CreateTableAsync<SettingEntity>();

            await MigrateFromPreferencesAsync();
            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    // ---------------------------------------------------------------
    // Настройки (Settings table)
    // ---------------------------------------------------------------

    public static async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        var entity = await Db.FindAsync<SettingEntity>(key);
        return entity?.Value ?? defaultValue;
    }

    public static async Task SetSettingAsync(string key, string value)
    {
        var existing = await Db.FindAsync<SettingEntity>(key);
        if (existing is not null)
        {
            existing.Value = value;
            await Db.UpdateAsync(existing);
        }
        else
        {
            await Db.InsertAsync(new SettingEntity { Key = key, Value = value });
        }
    }

    // ---------------------------------------------------------------
    // Миграция из Preferences
    // ---------------------------------------------------------------

    private static async Task MigrateFromPreferencesAsync()
    {
        var hasLocalUsers = Preferences.Default.Get("cb_accounts", string.Empty);
        if (string.IsNullOrWhiteSpace(hasLocalUsers))
            return;

        try
        {
            var usernames = JsonSerializer.Deserialize<List<string>>(hasLocalUsers) ?? new();
            foreach (var key in usernames)
            {
                if (await Db.FindAsync<UserEntity>(key) is not null)
                    continue;

                var user = new UserEntity
                {
                    UsernameKey = key,
                    DisplayName = Preferences.Default.Get($"cb_display_{key}", key),
                    Age = Preferences.Default.Get($"cb_age_{key}", 0),
                    AvatarEmoji = Preferences.Default.Get($"cb_avatar_{key}", "\U0001F9E0"),
                    PasswordHash = Preferences.Default.Get($"cb_pwd_{key}", string.Empty),
                    Onboarded = Preferences.Default.Get($"cb_onboarded_{key}", false),
                    PointsBalance = Preferences.Default.Get($"cb_points_balance_{key}", 0),
                    PointsLifetime = Preferences.Default.Get($"cb_points_lifetime_{key}", 0),
                    StreakLastDate = Preferences.Default.Get($"cb_streak_last_{key}", string.Empty),
                    StreakCurrent = Preferences.Default.Get($"cb_streak_current_{key}", 0),
                    StreakLongest = Preferences.Default.Get($"cb_streak_longest_{key}", 0),
                };

                var skillsRaw = Preferences.Default.Get($"cb_skills_{key}", string.Empty);
                if (!string.IsNullOrWhiteSpace(skillsRaw))
                    user.SkillsJson = skillsRaw;

                var gamesRaw = Preferences.Default.Get($"cb_unlocked_games_{key}", string.Empty);
                if (!string.IsNullOrWhiteSpace(gamesRaw))
                    user.UnlockedGamesJson = gamesRaw;

                var achievements = new Dictionary<string, string>();
                foreach (var achId in AllAchievementIds)
                {
                    var unlocked = Preferences.Default.Get($"cb_ach_u_{key}_{achId}", false);
                    if (unlocked)
                    {
                        var ts = Preferences.Default.Get($"cb_ach_t_{key}_{achId}", string.Empty);
                        achievements[achId] = ts;
                    }
                }
                if (achievements.Count > 0)
                    user.AchievementsJson = JsonSerializer.Serialize(achievements);

                await Db.InsertAsync(user);

                await MigrateGameHistoryAsync(key);
                await MigrateTestHistoryAsync(key);
            }

            await MigrateSettingAsync("cb_sound");
            await MigrateSettingAsync("cb_theme");
            await MigrateSettingAsync("cb_large_text");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DB] Migration error: {ex}");
        }
    }

    private static async Task MigrateGameHistoryAsync(string key)
    {
        var raw = Preferences.Default.Get($"cb_game_history_{key}", string.Empty);
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var games = JsonSerializer.Deserialize<List<GameResult>>(raw);
            if (games is null) return;
            foreach (var g in games)
            {
                await Db.InsertAsync(new GameResultEntity
                {
                    UsernameKey = key,
                    GameId = g.GameId,
                    GameTitle = g.GameTitle,
                    Skill = (int)g.Skill,
                    Score = g.Score,
                    MaxScore = g.MaxScore,
                    EarnedPoints = g.EarnedPoints,
                    PlayedAtUtc = g.PlayedAtUtc
                });
            }
        }
        catch { }
    }

    private static async Task MigrateTestHistoryAsync(string key)
    {
        var raw = Preferences.Default.Get($"cb_test_history_{key}", string.Empty);
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var tests = JsonSerializer.Deserialize<List<TestResult>>(raw);
            if (tests is null) return;
            foreach (var t in tests)
            {
                await Db.InsertAsync(new TestResultEntity
                {
                    UsernameKey = key,
                    TestId = t.TestId,
                    TestTitle = t.TestTitle,
                    CorrectAnswers = t.CorrectAnswers,
                    TotalQuestions = t.TotalQuestions,
                    IqScore = t.IqScore,
                    EarnedPoints = t.EarnedPoints,
                    PlayedAtUtc = t.PlayedAtUtc
                });
            }
        }
        catch { }
    }

    private static async Task MigrateSettingAsync(string key)
    {
        var val = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(val)) return;
        await SetSettingAsync(key, val);
    }

    private static readonly string[] AllAchievementIds =
    {
        "first_game","games_5","games_10","perfect_score","accuracy_90",
        "unlock_game","unlock_all","first_test","iq_100","iq_120","iq_130",
        "tests_5","brain_200","brain_500","brain_800","points_500","points_1000",
        "streak_3","streak_7","streak_30",
        "skill_memory_500","skill_focus_500","skill_lang_500","skill_logic_500"
    };
}
