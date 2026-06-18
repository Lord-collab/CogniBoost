using CogniBoost.Models;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Числовой ряд»: определи следующее число в арифметической/геометрической
/// закономерности. Вопросы генерируются случайно.
/// </summary>
public sealed class NumberSeriesGamePage : QuizGamePage
{
    private const int RoundCount = 6;

    public NumberSeriesGamePage()
        : base(GameCatalog.Get("number_series")!, Generate())
    {
    }

    private static IEnumerable<QuizQuestion> Generate()
    {
        var rng = new Random();
        var questions = new List<QuizQuestion>();

        for (var i = 0; i < RoundCount; i++)
        {
            questions.Add(BuildOne(rng));
        }

        return questions;
    }

    private static QuizQuestion BuildOne(Random rng)
    {
        // Тип закономерности: 0 — арифметическая, 1 — геометрическая, 2 — квадраты+смещение.
        var type = rng.Next(3);
        var start = rng.Next(1, 9);
        int[] sequence;
        int answer;

        switch (type)
        {
            case 0:
            {
                var step = rng.Next(2, 9);
                sequence = Enumerable.Range(0, 5).Select(n => start + n * step).ToArray();
                answer = start + 5 * step;
                break;
            }
            case 1:
            {
                var ratio = rng.Next(2, 4);
                sequence = Enumerable.Range(0, 5).Select(n => start * (int)Math.Pow(ratio, n)).ToArray();
                answer = start * (int)Math.Pow(ratio, 5);
                break;
            }
            default:
            {
                var step = rng.Next(1, 5);
                sequence = Enumerable.Range(0, 5).Select(n => (n + 1) * (n + 1) + step).ToArray();
                answer = 6 * 6 + step;
                break;
            }
        }

        var prompt = string.Join(", ", sequence) + ", ?";
        var options = BuildOptions(answer, rng);
        var correctIndex = Array.IndexOf(options, answer);

        return new QuizQuestion(prompt, options.Select(o => o.ToString()).ToArray(), correctIndex);
    }

    private static int[] BuildOptions(int answer, Random rng)
    {
        var set = new HashSet<int> { answer };
        while (set.Count < 4)
        {
            var delta = rng.Next(1, Math.Max(3, answer / 4 + 2));
            var candidate = rng.Next(2) == 0 ? answer + delta : answer - delta;
            if (candidate > 0)
            {
                set.Add(candidate);
            }
        }

        return set.OrderBy(_ => rng.Next()).ToArray();
    }
}
