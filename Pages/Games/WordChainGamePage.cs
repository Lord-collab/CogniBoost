using CogniBoost.Models;
using CogniBoost.Services;

namespace CogniBoost.Pages.Games;

public sealed class WordChainGamePage : QuizGamePage
{
    private static List<QuizQuestion>? _cache;

    private static async Task<List<QuizQuestion>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        var items = await ContentLoader.LoadListAsync<WordLink>("word_chain.json");
        _cache = items.Select(q => new QuizQuestion(q.From, q.Options, q.Correct)).ToList();
        return _cache;
    }

    public WordChainGamePage()
        : base(GameCatalog.Get("word_chain")!, Task.Run(LoadAsync).GetAwaiter().GetResult())
    {
    }

    private record WordLink(string From, string[] Options, int Correct);
}
