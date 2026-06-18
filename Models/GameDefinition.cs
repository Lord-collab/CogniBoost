namespace CogniBoost.Models;

/// <summary>
/// Определение игры в каталоге.
/// </summary>
public sealed record GameDefinition(
    string Id,
    string Title,
    string Description,
    BrainSkill Skill,
    string Emoji,
    Func<ContentPage> CreatePage,
    bool Starter = true,
    int UnlockCost = 0);
