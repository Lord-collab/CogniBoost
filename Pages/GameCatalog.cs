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
        new("reaction_tap",    "Быстрая реакция",
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

        // ── Новые игры ────────────────────────────────────────────────
        new("simon_says",    "Повтори ряд",
             "Запомни и повтори последовательность цветов",
             BrainSkill.Memory, "🔴", () => new SimonSaysGamePage(),     Starter: false, UnlockCost: 350),

        new("sudoku_mini",   "Судоку-мини",
             "Заполни сетку 4×4 цифрами без повторов",
             BrainSkill.Logic, "🔢", () => new SudokuMiniGamePage(),     Starter: false, UnlockCost: 400),

        new("balance_scale", "Весы",
             "Определи порядок предметов по весу",
             BrainSkill.Logic, "⚖️", () => new BalanceScaleGamePage(),   Starter: true),

        new("anagrams",      "Анаграммы",
             "Собери слово из перепутанных букв",
             BrainSkill.Language, "🧩", () => new AnagramGamePage(),     Starter: false, UnlockCost: 350),
     };

    public static GameDefinition? Get(string id)
        => All.FirstOrDefault(g => string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<GameDefinition> ForSkill(BrainSkill skill)
        => All.Where(g => g.Skill == skill).ToList();

    // ----------------------------------------------------------------
    // Тексты обучения для каждой игры
    // ----------------------------------------------------------------

    public static readonly IReadOnlyDictionary<string, string> TutorialTexts =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["memory_pairs"] = "Перед вами карточки, разложенные рубашкой вверх. " +
            "Нажимайте на две карточки — если они совпадают, пара остаётся открытой. " +
            "Соберите все пары за минимальное количество ходов.",

        ["color_sequence"] = "На экране загорится последовательность цветов. " +
            "Запомните её и повторите, нажимая на кнопки в правильном порядке. " +
            "С каждым уровнем последовательность становится длиннее.",

        ["number_recall"] = "Вам покажут число. Запомните его и введите " +
            "в поле ввода после того, как оно исчезнет. " +
            "С каждым уровнем число становится длиннее.",

        ["reaction_tap"] = "На экране будут появляться квадраты разных цветов. " +
            "Нажимайте ТОЛЬКО на зелёные. За красные и синие — штраф. " +
            "Реагируйте как можно быстрее!",

        ["spot_difference"] = "Перед вами список слов. Запомните его! " +
            "Затем появится новый список — найдите слово, " +
            "которого не было в предыдущем.",

        ["stroop_color"] = "Вам покажут слово, обозначающее цвет, " +
            "но написанное другим цветом. Ваша задача — выбрать цвет шрифта, " +
            "а не значение слова. Например: слово «Красный» написано синим — " +
            "правильный ответ «Синий».",

        ["number_series"] = "Дан ряд чисел, расположенных по определённой " +
            "закономерности. Найдите следующее число в ряду.",

        ["matrix_logic"] = "Дана аналогия: А относится к Б как В относится к ?. " +
            "Выберите четвёртый элемент, который завершает аналогию.",

        ["odd_word"] = "Из четырёх слов нужно выбрать одно, " +
            "которое выбивается из общего ряда по смыслу.",

        ["word_chain"] = "Вам дано слово. Выберите из вариантов " +
            "наиболее логичную ассоциацию к нему. " +
            "Каждый ваш выбор определяет следующее слово.",

        ["simon_says"] = "Запоминайте и повторяйте последовательность " +
            "цветов и звуков. С каждым раундом последовательность " +
            "увеличивается на один элемент. Ошибка — игра заканчивается.",

        ["sudoku_mini"] = "Заполните сетку 4×4 цифрами от 1 до 4 так, " +
            "чтобы в каждой строке, каждом столбце и каждом блоке 2×2 " +
            "цифры не повторялись.",

        ["balance_scale"] = "Даны несколько предметов и информация об их весе " +
            "(например, «A тяжелее B», «B легче C»). " +
            "Расположите предметы от самого лёгкого к самому тяжёлому.",

        ["anagrams"] = "Перед вами слово, в котором буквы перепутаны. " +
            "Составьте из них правильное слово, переставляя буквы. " +
            "Подсказка: тематика слов — когнитивные навыки и психология.",
    };

    public static string? GetTutorial(string gameId)
        => TutorialTexts.TryGetValue(gameId, out var text) ? text : null;
}
