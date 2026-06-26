using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

public sealed class MemoryPairsGamePage : GameBasePage
{
    private const int Pairs = 6;

    private static List<string>? _symbols;
    private readonly List<CardButton> _cards = new();
    private readonly Label _statusLabel = new();

    private CardButton? _firstPick;
    private int _matchedPairs;
    private int _attempts;
    private bool _busy;

    public MemoryPairsGamePage()
        : base(GameCatalog.Get("memory_pairs")!)
    {
        BuildUi();
    }

    private static List<string> GetSymbols()
    {
        if (_symbols is not null) return _symbols;
        _symbols = Task.Run(async () =>
            await ContentLoader.LoadListAsync<string>("memory_symbols.json"))
            .GetAwaiter().GetResult();
        if (_symbols is null || _symbols.Count == 0)
            _symbols = new List<string> { "🍎", "🚀", "⭐", "🎵", "🐱", "🌸", "⚽", "🎲", "🍕", "🌙" };
        return _symbols;
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _statusLabel.FontSize = 16;
        _statusLabel.TextColor = ThemeColors.TextSecondary;
        UpdateStatus();

        var rng = new Random();
        var symbols = GetSymbols().OrderBy(_ => rng.Next()).Take(Pairs).ToList();
        var deck = symbols.SelectMany(s => new[] { s, s }).OrderBy(_ => rng.Next()).ToList();

        var grid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        var rows = (int)Math.Ceiling(deck.Count / 3.0);
        for (var r = 0; r < rows; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var i = 0; i < deck.Count; i++)
        {
            var card = new CardButton(deck[i]);
            card.Tapped += OnCardTapped;
            _cards.Add(card);
            Grid.SetRow(card.View, i / 3);
            Grid.SetColumn(card.View, i % 3);
            grid.Children.Add(card.View);
        }

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 18,
                Children =
                {
                    new Label
                    {
                        Text = Definition.Title, FontSize = 22,
                        FontAttributes = FontAttributes.Bold, TextColor = Accent
                    },
                    _statusLabel,
                    grid
                }
            }
        };
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = $"Найдено пар: {_matchedPairs} из {Pairs} · Попыток: {_attempts}";
    }

    private async void OnCardTapped(CardButton card)
    {
        if (_busy || card.IsMatched || card == _firstPick || card.IsRevealed) return;
        card.Reveal();

        if (_firstPick is null) { _firstPick = card; return; }

        _busy = true;
        _attempts++;

        if (_firstPick.Symbol == card.Symbol)
        {
            _firstPick.SetMatched(Accent);
            card.SetMatched(Accent);
            _matchedPairs++;
            _firstPick = null;
            UpdateStatus();
            HapticService.Click();
            SoundService.PlayCorrect();
            _ = card.View.PopAsync();
            if (_matchedPairs >= Pairs) { await Task.Delay(400); await FinishWithScore(); }
        }
        else
        {
            UpdateStatus();
            HapticService.Error();
            SoundService.PlayWrong();
            _ = card.View.ShakeAsync();
            await Task.Delay(800);
            _firstPick.Hide();
            card.Hide();
            _firstPick = null;
        }

        _busy = false;
    }

    private async Task FinishWithScore()
    {
        var maxScore = 100;
        var penalty = Math.Max(0, _attempts - Pairs) * 8;
        var score = Math.Clamp(maxScore - penalty, 10, maxScore);
        await FinishAsync(score, maxScore);
    }

    private sealed class CardButton
    {
        private readonly Border _border;
        private readonly Label _label;

        public CardButton(string symbol)
        {
            Symbol = symbol;
            _label = new Label
            {
                Text = string.Empty, FontSize = 32,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            _border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Stroke = Colors.Transparent,
                BackgroundColor = ThemeColors.AccentLight,
                HeightRequest = 88,
                Content = _label
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => Tapped?.Invoke(this);
            _border.GestureRecognizers.Add(tap);
        }

        public string Symbol { get; }
        public View View => _border;
        public bool IsMatched { get; private set; }
        public bool IsRevealed { get; private set; }
        public event Action<CardButton>? Tapped;

        public void Reveal()
        {
            IsRevealed = true;
            _label.Text = Symbol;
            _border.BackgroundColor = ThemeColors.CardBg;
        }

        public void Hide()
        {
            IsRevealed = false;
            _label.Text = string.Empty;
            _border.BackgroundColor = ThemeColors.AccentLight;
        }

        public void SetMatched(Color accent)
        {
            IsMatched = true;
            IsRevealed = true;
            _label.Text = Symbol;
            _border.BackgroundColor = accent.WithAlpha(0.25f);
        }
    }
}
