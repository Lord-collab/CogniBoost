using CogniBoost.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Найди пары»: сетка карточек, нужно открыть все совпадающие пары.
/// Счёт зависит от числа попыток (меньше промахов — выше точность).
/// </summary>
public sealed class MemoryPairsGamePage : GameBasePage
{
    private const int Pairs = 6; // 12 карточек, сетка 3x4

    private static readonly string[] Symbols =
        { "🍎", "🚀", "⭐", "🎵", "🐱", "🌸", "⚽", "🎲", "🍕", "🌙" };

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

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _statusLabel.FontSize = 16;
        _statusLabel.TextColor = Color.FromArgb("#6B7280");
        UpdateStatus();

        var rng = new Random();
        var deck = Symbols.Take(Pairs)
            .SelectMany(s => new[] { s, s })
            .OrderBy(_ => rng.Next())
            .ToList();

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
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

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
                        Text = Definition.Title,
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Accent
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
        if (_busy || card.IsMatched || card == _firstPick || card.IsRevealed)
        {
            return;
        }

        card.Reveal();

        if (_firstPick is null)
        {
            _firstPick = card;
            return;
        }

        _busy = true;
        _attempts++;

        if (_firstPick.Symbol == card.Symbol)
        {
            _firstPick.SetMatched(Accent);
            card.SetMatched(Accent);
            _matchedPairs++;
            _firstPick = null;
            UpdateStatus();

            if (_matchedPairs >= Pairs)
            {
                await Task.Delay(400);
                await FinishWithScore();
            }
        }
        else
        {
            UpdateStatus();
            await Task.Delay(800);
            _firstPick.Hide();
            card.Hide();
            _firstPick = null;
        }

        _busy = false;
    }

    private async Task FinishWithScore()
    {
        // Идеал — закрыть все пары за Pairs попыток. Лишние попытки снижают счёт.
        var maxScore = 100;
        var penalty = Math.Max(0, _attempts - Pairs) * 8;
        var score = Math.Clamp(maxScore - penalty, 10, maxScore);
        await FinishAsync(score, maxScore);
    }

    /// <summary>Обёртка над карточкой-кнопкой памяти.</summary>
    private sealed class CardButton
    {
        private readonly Border _border;
        private readonly Label _label;

        public CardButton(string symbol)
        {
            Symbol = symbol;
            _label = new Label
            {
                Text = string.Empty,
                FontSize = 32,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            _border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Stroke = Colors.Transparent,
                BackgroundColor = Color.FromArgb("#E0E7FF"),
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
            _border.BackgroundColor = Colors.White;
        }

        public void Hide()
        {
            IsRevealed = false;
            _label.Text = string.Empty;
            _border.BackgroundColor = Color.FromArgb("#E0E7FF");
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
