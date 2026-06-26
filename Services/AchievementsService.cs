using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public sealed record Achievement(
    string Id,
    string Title,
    string Description,
    string Emoji,
    bool IsUnlocked,
    DateTime? UnlockedAt);

public static class AchievementsService
{
    private static readonly (string Id, string Title, string Description, string Emoji)[] Definitions =
    {
        ("first_game",       "Первый шаг",        "Сыграй первую игру",                    "\uD83C\uDFAE"),
        ("games_5",          "На разогреве",       "Сыграй 5 разных игр",                   "\uD83D\uDD25"),
        ("games_10",         "Игроман",            "Сыграй 10 разных игр",                  "\uD83D\uDD79\uFE0F"),
        ("perfect_score",    "Перфекционист",      "Набери 100% точности в любой игре",     "\uD83D\uDC8E"),
        ("accuracy_90",      "Острый ум",          "Набери 90%+ точности три раза подряд",  "\uD83E\uDDE0"),
        ("unlock_game",      "Коллекционер",       "Открой платную игру",                   "\uD83D\uDD13"),
        ("unlock_all",       "Всё включено",       "Открой все игры в каталоге",            "\uD83C\uDFC6"),
        ("first_test",       "Тест пройден",       "Прохди первый IQ-тест",                 "\uD83D\uDCDD"),
        ("iq_100",           "Средний уровень",    "Набери IQ 100 или выше",                "\uD83D\uDCA1"),
        ("iq_120",           "Высокий интеллект",  "Набери IQ 120 или выше",                "\uD83C\uDF1F"),
        ("iq_130",           "Гений",              "Набери IQ 130 или выше",                "\uD83C\uDFC5"),
        ("tests_5",          "Испытатель",         "Пройди 5 тестов",                       "\uD83D\uDD2C"),
        ("brain_200",        "Начало пути",        "Набери 200 очков индекса мозга",        "\uD83D\uDCC8"),
        ("brain_500",        "Полпути",            "Набери 500 очков индекса мозга",        "\uD83D\uDE80"),
        ("brain_800",        "Мастер разума",      "Набери 800 очков индекса мозга",        "\uD83C\uDFAF"),
        ("points_500",       "Богач",              "Заработай 500 бонусных очков",          "\u2B50"),
        ("points_1000",      "Миллионер звёзд",   "Заработай 1000 бонусных очков",         "\uD83D\uDCAB"),
        ("streak_3",         "3 дня подряд",       "Тренируйся 3 дня без перерыва",         "\uD83D\uDD25"),
        ("streak_7",         "Неделя силы",        "Тренируйся 7 дней без перерыва",        "\uD83D\uDCAA"),
        ("streak_30",        "Железная воля",      "Тренируйся 30 дней без перерыва",       "\uD83C\uDFCB\uFE0F"),
        ("skill_memory_500", "Запоминатор",        "Доведи навык Память до 500",            "\uD83E\uDDE9"),
        ("skill_focus_500",  "Фокусник",           "Доведи навык Внимание до 500",          "\uD83C\uDFAF"),
        ("skill_lang_500",   "Словарь",            "Доведи навык Язык до 500",              "\uD83D\uDCDA"),
        ("skill_logic_500",  "Логик",              "Доведи навык Логика до 500",            "\u2699\uFE0F"),
    };

    public static IReadOnlyList<Achievement> GetAll()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            var achievements = LoadAchievements(user?.AchievementsJson);

            return Definitions.Select(d =>
            {
                var hasTimestamp = achievements.TryGetValue(d.Id, out var timestamp);
                DateTime? date = hasTimestamp && DateTime.TryParse(timestamp, out var dt) ? dt : null;
                return new Achievement(d.Id, d.Title, d.Description, d.Emoji, hasTimestamp, date);
            }).ToList();
        });
    }

    public static int UnlockedCount() => GetAll().Count(a => a.IsUnlocked);
    public static int TotalCount() => Definitions.Length;

    public static IReadOnlyList<Achievement> CheckAndUnlock()
    {
        var newly = new List<Achievement>();

        var history = ProgressStore.GetGameHistory();
        var testHistory = ProgressStore.GetTestHistory();
        var playedGames = history.Select(r => r.GameId)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var overall = ProgressStore.GetOverallScore();
        var lifetime = PointsService.GetLifetimeEarned();
        var streak = StreakService.GetCurrentStreak();
        var unlockedGames = Pages.GameCatalog.All.Count(g => UnlockService.IsUnlocked(g));
        var lockedCount = Pages.GameCatalog.All.Count(g => !g.Starter);

        TryUnlock("first_game", history.Count >= 1, newly);
        TryUnlock("games_5", playedGames >= 5, newly);
        TryUnlock("games_10", playedGames >= 10, newly);
        TryUnlock("perfect_score", history.Any(r => r.AccuracyPercent >= 100), newly);

        var last3 = history.Take(3).ToList();
        TryUnlock("accuracy_90", last3.Count == 3 && last3.All(r => r.AccuracyPercent >= 90), newly);

        TryUnlock("unlock_game", Pages.GameCatalog.All.Any(g => !g.Starter && UnlockService.IsUnlocked(g)), newly);
        TryUnlock("unlock_all", lockedCount > 0 && Pages.GameCatalog.All.Where(g => !g.Starter).All(g => UnlockService.IsUnlocked(g)), newly);

        TryUnlock("first_test", testHistory.Count >= 1, newly);
        TryUnlock("iq_100", testHistory.Any(t => t.IqScore >= 100), newly);
        TryUnlock("iq_120", testHistory.Any(t => t.IqScore >= 120), newly);
        TryUnlock("iq_130", testHistory.Any(t => t.IqScore >= 130), newly);
        TryUnlock("tests_5", testHistory.Count >= 5, newly);

        TryUnlock("brain_200", overall >= 200, newly);
        TryUnlock("brain_500", overall >= 500, newly);
        TryUnlock("brain_800", overall >= 800, newly);
        TryUnlock("points_500", lifetime >= 500, newly);
        TryUnlock("points_1000", lifetime >= 1000, newly);

        TryUnlock("streak_3", streak >= 3, newly);
        TryUnlock("streak_7", streak >= 7, newly);
        TryUnlock("streak_30", streak >= 30, newly);

        TryUnlock("skill_memory_500", ProgressStore.GetSkillScore(BrainSkill.Memory) >= 500, newly);
        TryUnlock("skill_focus_500", ProgressStore.GetSkillScore(BrainSkill.Focus) >= 500, newly);
        TryUnlock("skill_lang_500", ProgressStore.GetSkillScore(BrainSkill.Language) >= 500, newly);
        TryUnlock("skill_logic_500", ProgressStore.GetSkillScore(BrainSkill.Logic) >= 500, newly);

        return newly;
    }

    private static void TryUnlock(string id, bool condition, List<Achievement> newly)
    {
        if (!condition) return;

        var isNew = DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            var dict = LoadAchievements(user?.AchievementsJson);
            if (dict.ContainsKey(id))
                return false;

            dict[id] = DateTime.UtcNow.ToString("O");
            if (user is not null)
            {
                user.AchievementsJson = JsonSerializer.Serialize(dict);
                await DatabaseService.Db.UpdateAsync(user);
            }
            return true;
        });

        if (isNew)
        {
            var def = Definitions.FirstOrDefault(d => d.Id == id);
            if (def != default)
                newly.Add(new Achievement(def.Id, def.Title, def.Description,
                    def.Emoji, true, DateTime.UtcNow));
        }
    }

    private static Dictionary<string, string> LoadAchievements(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static string UserKey()
    {
        var k = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(k) ? "guest" : k;
    }
}
