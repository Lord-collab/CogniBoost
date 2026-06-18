namespace CogniBoost.Models;

/// <summary>
/// Вопрос теста с вариантами ответа.
/// </summary>
public sealed record TestQuestion(string Prompt, string[] Options, int CorrectIndex);

/// <summary>
/// Определение теста (например, экспресс IQ-тест).
/// </summary>
public sealed record TestDefinition(
    string Id,
    string Title,
    string Description,
    int DurationSeconds,
    Func<IReadOnlyList<TestQuestion>> BuildQuestions);
