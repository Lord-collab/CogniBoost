namespace CogniBoost.Models;

/// <summary>
/// Результат одной игровой сессии.
/// </summary>
public sealed record GameResult(
    string GameId,
    string GameTitle,
    BrainSkill Skill,
    int Score,
    int MaxScore,
    int EarnedPoints,
    DateTime PlayedAtUtc)
{
    /// <summary>Нормализованная доля от максимума [0..1].</summary>
    public double Accuracy => MaxScore > 0 ? Math.Clamp(Score / (double)MaxScore, 0, 1) : 0;

    public int AccuracyPercent => (int)Math.Round(Accuracy * 100);
}
