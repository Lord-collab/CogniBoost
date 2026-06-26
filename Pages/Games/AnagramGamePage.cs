using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

public sealed class AnagramGamePage : GameBasePage
{
    private const int Rounds = 6;

    private static List<AnagramWord>? _words;
    private readonly Random _rng = new();
    private readonly Label _progressLabel = new();
    private readonly Label _anagramLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Entry _answerEntry = new();
    private readonly Label _scoreLabel = new();

    private List<int> _usedIndices = new();
    private int _currentIndex;
    private int _round;
    private int _score;

    public AnagramGamePage()
        : base(GameCatalog.Get("anagrams")!)
    {
        BuildUi();
        _ = LoadAndStartAsync();
    }

    private async Task LoadAndStartAsync()
    {
        if (_words is null)
        {
            var loaded = await ContentLoader.LoadListAsync<AnagramWord>("anagram_words.json");
            _words = loaded.Count > 0 ? loaded : DefaultWords();
        }
        NextAnagram();
    }

    private static List<AnagramWord> DefaultWords() => new()
    {
        new("СТОЛ", "Мебель"), new("КНИГА", "Читают"), new("МОРЕ", "Вода"),
        new("ГОРА", "Высота"), new("ШКОЛА", "Учёба"), new("САД", "Деревья"),
    };

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;

        _anagramLabel.FontSize = 36;
        _anagramLabel.FontAttributes = FontAttributes.Bold;
        _anagramLabel.HorizontalOptions = LayoutOptions.Center;
        _anagramLabel.TextColor = ThemeColors.TextPrimary;
        _anagramLabel.HorizontalTextAlignment = TextAlignment.Center;
        _anagramLabel.CharacterSpacing = 8;

        _hintLabel.FontSize = 15;
        _hintLabel.TextColor = ThemeColors.TextMuted;
        _hintLabel.HorizontalOptions = LayoutOptions.Center;

        _scoreLabel.FontSize = 16;
        _scoreLabel.TextColor = ThemeColors.TextSecondary;
        _scoreLabel.HorizontalOptions = LayoutOptions.Center;

        _answerEntry.Placeholder = "Твой ответ";
        _answerEntry.FontSize = 22;
        _answerEntry.HorizontalTextAlignment = TextAlignment.Center;
        _answerEntry.BackgroundColor = ThemeColors.CardBg;
        _answerEntry.TextColor = ThemeColors.TextPrimary;
        _answerEntry.Completed += OnSubmit;

        var submitBtn = new Button
        {
            Text = "Проверить",
            BackgroundColor = Accent,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 52,
            CornerRadius = 14
        };
        submitBtn.Clicked += OnSubmit;

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 24,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "🧩", FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                    _anagramLabel,
                    _hintLabel,
                }
            }
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 16,
            Children =
            {
                new Label
                {
                    Text = Definition.Title, FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Accent,
                    HorizontalOptions = LayoutOptions.Center
                },
                _progressLabel,
                _scoreLabel,
                card,
                _answerEntry,
                submitBtn,
            }
        };
    }

    private void NextAnagram()
    {
        var words = _words ?? DefaultWords();
        var available = Enumerable.Range(0, words.Count).Where(i => !_usedIndices.Contains(i)).ToList();
        if (available.Count == 0)
        {
            _usedIndices.Clear();
            available = Enumerable.Range(0, words.Count).ToList();
        }

        _currentIndex = available[_rng.Next(available.Count)];
        _usedIndices.Add(_currentIndex);

        var (word, hint) = words[_currentIndex];
        var shuffled = word.ToCharArray().OrderBy(_ => _rng.Next()).ToArray();

        _progressLabel.Text = $"Слово {_round + 1} из {Rounds}";
        _anagramLabel.Text = new string(shuffled);
        _hintLabel.Text = $"Подсказка: {hint}";
        _answerEntry.Text = "";
        _answerEntry.Focus();
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        var words = _words ?? DefaultWords();
        var answer = _answerEntry.Text?.Trim().ToUpperInvariant() ?? "";
        var (word, _) = words[_currentIndex];

        if (string.Equals(answer, word, StringComparison.Ordinal))
        {
            _score++;
            _anagramLabel.TextColor = ThemeColors.Success;
            HapticService.Click();
            SoundService.PlayCorrect();
            _ = _anagramLabel.PopAsync();
        }
        else
        {
            _anagramLabel.TextColor = ThemeColors.Error;
            _anagramLabel.Text = word;
            HapticService.Error();
            SoundService.PlayWrong();
            _ = _anagramLabel.ShakeAsync();
        }

        await Task.Delay(800);

        _round++;
        if (_round >= Rounds)
        {
            await FinishAsync(_score, Rounds);
            return;
        }

        _anagramLabel.TextColor = ThemeColors.TextPrimary;
        NextAnagram();
    }

    private record AnagramWord(string Word, string Hint);
}
