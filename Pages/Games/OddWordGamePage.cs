using CogniBoost.Models;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Лишнее слово»: из четырёх слов одно не подходит к остальным по категории.
/// </summary>
public sealed class OddWordGamePage : QuizGamePage
{
    public OddWordGamePage()
        : base(GameCatalog.Get("odd_word")!, Generate())
    {
    }

    // Группы связанных слов; к каждой добавляется одно "лишнее" из другой группы.
    private static readonly string[][] Groups =
    {
        new[] { "Яблоко", "Груша", "Слива", "Банан", "Персик" },
        new[] { "Собака", "Кошка", "Лошадь", "Корова", "Овца" },
        new[] { "Красный", "Синий", "Зелёный", "Жёлтый", "Чёрный" },
        new[] { "Молоток", "Отвёртка", "Пила", "Дрель", "Гаечный ключ" },
        new[] { "Роза", "Тюльпан", "Ромашка", "Лилия", "Пион" },
        new[] { "Москва", "Париж", "Берлин", "Токио", "Рим" },
        new[] { "Скрипка", "Гитара", "Пианино", "Флейта", "Барабан" },
        new[] { "Круг", "Квадрат", "Треугольник", "Ромб", "Овал" },
    };

    private static IEnumerable<QuizQuestion> Generate()
    {
        var rng = new Random();
        var order = Enumerable.Range(0, Groups.Length).OrderBy(_ => rng.Next()).Take(6).ToList();
        var questions = new List<QuizQuestion>();

        foreach (var groupIndex in order)
        {
            var group = Groups[groupIndex];

            // Три слова из этой группы + одно из другой группы.
            var same = group.OrderBy(_ => rng.Next()).Take(3).ToList();

            var otherGroupIndex = rng.Next(Groups.Length);
            while (otherGroupIndex == groupIndex)
            {
                otherGroupIndex = rng.Next(Groups.Length);
            }

            var odd = Groups[otherGroupIndex][rng.Next(Groups[otherGroupIndex].Length)];

            var options = new List<string>(same) { odd };
            var shuffled = options.OrderBy(_ => rng.Next()).ToArray();
            var correctIndex = Array.IndexOf(shuffled, odd);

            questions.Add(new QuizQuestion("Найди лишнее слово", shuffled, correctIndex));
        }

        return questions;
    }
}
