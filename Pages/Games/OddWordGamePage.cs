using CogniBoost.Models;
using CogniBoost.Services;

namespace CogniBoost.Pages.Games;

public sealed class OddWordGamePage : QuizGamePage
{
    private static List<QuizQuestion>? _cache;

    private static async Task<List<QuizQuestion>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        var items = await ContentLoader.LoadListAsync<OddQ>("odd_word_questions.json");
        _cache = items.Select(q => new QuizQuestion(q.Prompt, q.Options, q.Correct)).ToList();
        return _cache;
    }

    public OddWordGamePage()
        : base(GameCatalog.Get("odd_word")!, Task.Run(LoadAsync).GetAwaiter().GetResult())
    {
    }

    private record OddQ(string Prompt, string[] Options, int Correct);
}
