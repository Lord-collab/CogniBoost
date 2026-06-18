using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Одно достижение.
/// </summary>
public sealed record Achievement(
    string Id,
    string Title,
    string Description,
    string Emoji,
    bool IsUnlocked,
    DateTime? UnlockedAt);

/// <summary>
/// Сервис достижений: хранение, проверка условий, уведомление.
/// </summary>
public static class AchievementsService
{
    private const string Prefix = "cb_ach_";

    // Статические определения всех достижений
    private static readonly (string Id, string Title, string Description, string Emoji)[] Definitions =
    {
        // Игровые
        ("first_game",       "Первый шаг",        "Сыграй первую игру",                    "🎮"),
        ("games_5",          "На разогреве",       "Сыграй 5 разных игр",                   "🔥"),
        ("games_10",         "Игроман",            "Сыграй 10 разных игр",                  "🕹️"),
        ("perfect_score",    "Перфекционист",      "Набери 100% точности в любой игре",     "💎"),
        ("accuracy_90",      "Острый ум",          "Набери 90%+ точности три раза подряд",  "🧠"),
        ("unlock_game",      "Коллекционер",       "Открой платную игру",                   "🔓"),
        ("unlock_all",       "Всё включено",       "Открой все игры в каталоге",            "🏆"),

        // Тесты
        ("first_test",       "Тест пройден",       "Прохди первый IQ-тест",                 "📝"),
        ("iq_100",           "Средний уровень",    "Набери IQ 100 или выше",                "💡"),
        ("iq_120",           "Высокий интеллект",  "Набери IQ 120 или выше",                "🌟"),
        ("iq_130",           "Гений",              "Набери IQ 130 или выше",                "🏅"),
        ("tests_5",          "Испытатель",         "Пройди 5 тестов",                       "🔬"),

        // Прогресс
        ("brain_200",        "Начало пути",        "Набери 200 очков индекса мозга",        "📈"),
        ("brain_500",        "Полпути",            "Набери 500 очков индекса мозга",        "🚀"),
        ("brain_800",        "Мастер разума",      "Набери 800 очков индекса мозга",        "🎯"),
        ("points_500",       "Богач",              "Заработай 500 бонусных очков",          "⭐"),
        ("points_1000",      "Миллионер звёзд",   "Заработай 1000 бонусных очков",         "💫"),

        // Streak
        ("streak_3",         "3 дня подряд",       "Тренируйся 3 дня без перерыва",         "🔥"),
        ("streak_7",         "Неделя силы",        "Тренируйся 7 дней без перерыва",        "💪"),
        ("streak_30",        "Железная воля",      "Тренируйся 30 дней без перерыва",       "🏋️"),

        // Навыки
        ("skill_memory_500", "Запоминатор",        "Доведи навык Память до 500",            "🧩"),
        ("skill_focus_500",  "Фокусник",           "Доведи навык Внимание до 500",          "🎯"),
        ("skill_lang_500",   "Словарь",            "Доведи навык Язык до 500",              "📚"),
        ("skill_logic_500",  "Логик",              "Доведи навык Логика до 500",            "⚙️"),
    };

    /// <summary>Получить все достижения с актуальным статусом разблокировки.</summary>
    public static IReadOnlyList<Achievement> GetAll()
    {
        return Definitions.Select(d =>
        {
            var unlocked  = Preferences.Default.Get($"{Prefix}u_{UserKey()}_{d.Id}", false);
            var timestamp = Preferences.Default.Get($"{Prefix}t_{UserKey()}_{d.Id}", string.Empty);
            DateTime? date = string.IsNullOrWhiteSpace(timestamp) ? null
                : DateTime.TryParse(timestamp, out var dt) ? dt : null;
            return new Achievement(d.Id, d.Title, d.Description, d.Emoji, unlocked, date);
        }).ToList();
    }

    public static int UnlockedCount() => GetAll().Count(a => a.IsUnlocked);
    public static int TotalCount()    => Definitions.Length;

    /// <summary>
    /// Проверяет все условия и разблокирует новые достижения.
    /// Возвращает список свежеразблокированных (для показа попапа).
    /// </summary>
    public static IReadOnlyList<Achievement> CheckAndUnlock()
    {
        var newly = new List<Achievement>();

        var history      = ProgressStore.GetGameHistory();
        var testHistory  = ProgressStore.GetTestHistory();
        var playedGames  = history.Select(r => r.GameId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var overall      = ProgressStore.GetOverallScore();
        var lifetime     = PointsService.GetLifetimeEarned();
        var streak       = StreakService.GetCurrentStreak();
        var unlockedGames= Pages.GameCatalog.All.Count(g => UnlockService.IsUnlocked(g));
        var lockedCount  = Pages.GameCatalog.All.Count(g => !g.Starter);

        // ── Игровые ──────────────────────────────────────────
        TryUnlock("first_game",    history.Count >= 1, newly);
        TryUnlock("games_5",       playedGames >= 5, newly);
        TryUnlock("games_10",      playedGames >= 10, newly);
        TryUnlock("perfect_score", history.Any(r => r.AccuracyPercent >= 100), newly);

        // 90%+ три раза подряд
        var last3 = history.Take(3).ToList();
        TryUnlock("accuracy_90",   last3.Count == 3 && last3.All(r => r.AccuracyPercent >= 90), newly);

        TryUnlock("unlock_game",   Pages.GameCatalog.All.Any(g => !g.Starter && UnlockService.IsUnlocked(g)), newly);
        TryUnlock("unlock_all",    lockedCount > 0 && Pages.GameCatalog.All.Where(g => !g.Starter).All(g => UnlockService.IsUnlocked(g)), newly);

        // ── Тесты ─────────────────────────────────────────────
        TryUnlock("first_test",    testHistory.Count >= 1, newly);
        TryUnlock("iq_100",        testHistory.Any(t => t.IqScore >= 100), newly);
        TryUnlock("iq_120",        testHistory.Any(t => t.IqScore >= 120), newly);
        TryUnlock("iq_130",        testHistory.Any(t => t.IqScore >= 130), newly);
        TryUnlock("tests_5",       testHistory.Count >= 5, newly);

        // ── Прогресс ──────────────────────────────────────────
        TryUnlock("brain_200",     overall >= 200, newly);
        TryUnlock("brain_500",     overall >= 500, newly);
        TryUnlock("brain_800",     overall >= 800, newly);
        TryUnlock("points_500",    lifetime >= 500, newly);
        TryUnlock("points_1000",   lifetime >= 1000, newly);

        // ── Streak ────────────────────────────────────────────
        TryUnlock("streak_3",      streak >= 3, newly);
        TryUnlock("streak_7",      streak >= 7, newly);
        TryUnlock("streak_30",     streak >= 30, newly);

        // ── Навыки ────────────────────────────────────────────
        TryUnlock("skill_memory_500", ProgressStore.GetSkillScore(BrainSkill.Memory)   >= 500, newly);
        TryUnlock("skill_focus_500",  ProgressStore.GetSkillScore(BrainSkill.Focus)    >= 500, newly);
        TryUnlock("skill_lang_500",   ProgressStore.GetSkillScore(BrainSkill.Language) >= 500, newly);
        TryUnlock("skill_logic_500",  ProgressStore.GetSkillScore(BrainSkill.Logic)    >= 500, newly);

        return newly;
    }

    private static void TryUnlock(string id, bool condition, List<Achievement> newly)
    {
        if (!condition) return;
        var key = $"{Prefix}u_{UserKey()}_{id}";
        if (Preferences.Default.Get(key, false)) return; // уже разблокировано

        Preferences.Default.Set(key, true);
        Preferences.Default.Set($"{Prefix}t_{UserKey()}_{id}", DateTime.UtcNow.ToString("O"));

        var def = Definitions.FirstOrDefault(d => d.Id == id);
        if (def != default)
            newly.Add(new Achievement(def.Id, def.Title, def.Description, def.Emoji,
                true, DateTime.UtcNow));
    }

    private static string UserKey()
    {
        var k = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(k) ? "guest" : k;
    }
}
