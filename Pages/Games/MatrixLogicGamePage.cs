using CogniBoost.Models;
using CogniBoost.Services;

namespace CogniBoost.Pages.Games;

public sealed class MatrixLogicGamePage : QuizGamePage
{
    private static List<QuizQuestion>? _cache;

    private static async Task<List<QuizQuestion>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        var items = await ContentLoader.LoadListAsync<MatrixQ>("matrix_questions.json");
        _cache = items.Select(q => new QuizQuestion(q.Prompt, q.Options, q.Correct)).ToList();
        return _cache;
    }

    public MatrixLogicGamePage()
        : base(GameCatalog.Get("matrix_logic")!, Task.Run(LoadAsync).GetAwaiter().GetResult())
    {
    }

    private record MatrixQ(string Prompt, string[] Options, int Correct);
}
