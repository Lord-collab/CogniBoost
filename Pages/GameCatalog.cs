using CogniBoost.Models;
using CogniBoost.Pages.Games;

namespace CogniBoost.Pages;

/// <summary>
/// Каталог всех игр. Стартовые доступны сразу; остальные открываются за бонусы.
/// </summary>
public static class GameCatalog
{
    public static readonly IReadOnlyList<GameDefinition> All = new List<GameDefinition>
    {
        // ── Память ───────────────────────────────────────────────────
        new("memory_pairs",    "Найди пары",
            "Запомни расположение и собери пары карточек",
            BrainSkill.Memory, "🃏", () => new MemoryPairsGamePage(),   Starter: true),

        new("color_sequence",  "Цветовая память",
            "Запомни и повтори последовательность цветов",
            BrainSkill.Memory, "🌈", () => new ColorSequenceGamePage(), Starter: true),

        new("number_recall",   "Запомни число",
            "Запомни и воспроизведи числовую последовательность",
            BrainSkill.Memory, "🔢", () => new NumberRecallGamePage(),  Starter: false, UnlockCost: 300),

        // ── Внимание ─────────────────────────────────────────────────
        new("reaction_tap",    "Быстрый тап",
            "Реагируй только на нужный цвет как можно быстрее",
            BrainSkill.Focus, "⚡", () => new ReactionGamePage(),       Starter: true),

        new("spot_difference", "Найди изменение",
            "Найди слово, которое появилось в новом списке",
            BrainSkill.Focus, "🔍", () => new SpotDifferenceGamePage(), Starter: true),

        new("stroop_color",    "Истинный цвет",
            "Выбирай цвет шрифта, а не значение слова",
            BrainSkill.Focus, "🎨", () => new StroopGamePage(),         Starter: false, UnlockCost: 400),

        // ── Логика ───────────────────────────────────────────────────
        new("number_series",   "Числовой ряд",
            "Определи следующее число в закономерности",
            BrainSkill.Logic, "🧩", () => new NumberSeriesGamePage(),   Starter: true),

        new("matrix_logic",    "Матрица",
            "Найди аналогию: А:Б = В:?",
            BrainSkill.Logic, "🔳", () => new MatrixLogicGamePage(),    Starter: false, UnlockCost: 350),

        // ── Язык ─────────────────────────────────────────────────────
        new("odd_word",        "Лишнее слово",
            "Найди слово, которое выбивается из ряда",
            BrainSkill.Language, "💬", () => new OddWordGamePage(),     Starter: true),

        new("word_chain",      "Цепочка слов",
            "Выбирай лучшую ассоциацию к каждому слову",
            BrainSkill.Language, "🔗", () => new WordChainGamePage(),   Starter: false, UnlockCost: 300),
    };

    public static GameDefinition? Get(string id)
        => All.FirstOrDefault(g => string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<GameDefinition> ForSkill(BrainSkill skill)
        => All.Where(g => g.Skill == skill).ToList();
}
