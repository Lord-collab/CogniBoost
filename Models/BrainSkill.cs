namespace CogniBoost.Models;

/// <summary>
/// Когнитивные навыки (категории), которые развивает приложение.
/// </summary>
public enum BrainSkill
{
    Memory,
    Focus,
    Language,
    Logic
}

public static class BrainSkillInfo
{
    public sealed record SkillMeta(
        BrainSkill Skill,
        string Title,
        string Description,
        string Emoji,
        string AccentHex);

    public static readonly IReadOnlyList<SkillMeta> All = new[]
    {
        new SkillMeta(BrainSkill.Memory, "Память", "Запоминание и воспроизведение информации", "\U0001F9E0", "#3B82F6"),
        new SkillMeta(BrainSkill.Focus, "Внимание", "Концентрация и скорость реакции", "\U0001F3AF", "#10B981"),
        new SkillMeta(BrainSkill.Language, "Язык", "Словарный запас и работа со словами", "\U0001F4AC", "#F97316"),
        new SkillMeta(BrainSkill.Logic, "Логика", "Решение задач и мышление", "\U0001F9E9", "#8B5CF6"),
    };

    public static SkillMeta Get(BrainSkill skill) => All.First(s => s.Skill == skill);

    public static string Title(BrainSkill skill) => Get(skill).Title;

    public static string Emoji(BrainSkill skill) => Get(skill).Emoji;

    public static Color Accent(BrainSkill skill) => Color.FromArgb(Get(skill).AccentHex);
}
