namespace CogniBoost.Models;

/// <summary>
/// Результат прохождения теста (например, IQ-теста).
/// </summary>
public sealed record TestResult(
    string TestId,
    string TestTitle,
    int CorrectAnswers,
    int TotalQuestions,
    int IqScore,
    int EarnedPoints,
    DateTime PlayedAtUtc)
{
    public int AccuracyPercent => TotalQuestions > 0
        ? (int)Math.Round(CorrectAnswers / (double)TotalQuestions * 100)
        : 0;
}
