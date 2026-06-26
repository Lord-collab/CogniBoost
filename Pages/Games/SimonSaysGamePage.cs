using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

public sealed class SimonSaysGamePage : GameBasePage
{
    private const int Rounds = 6;

    private static readonly (string Name, Color Color)[] Buttons =
    {
        ("Красный",  Color.FromArgb("#EF4444")),
        ("Синий",    Color.FromArgb("#3B82F6")),
        ("Зелёный",  Color.FromArgb("#10B981")),
        ("Жёлтый",   Color.FromArgb("#F59E0B")),
    };

    private readonly Label _statusLabel = new();
    private readonly Label _scoreLabel = new();
    private readonly List<Border> _buttonViews = new();
    private readonly Random _rng = new();

    private List<int> _sequence = new();
    private int _playerIndex;
    private int _round;
    private int _score;
    private bool _showing;

    public SimonSaysGamePage()
        : base(GameCatalog.Get("simon_says")!)
    {
        BuildUi();
        _ = StartRoundAsync();
    }

    private void BuildUi()
    {
        _statusLabel.FontSize = 16;
        _statusLabel.TextColor = ThemeColors.TextSecondary;
        _statusLabel.HorizontalOptions = LayoutOptions.Center;

        _scoreLabel.FontSize = 18;
        _scoreLabel.FontAttributes = FontAttributes.Bold;
        _scoreLabel.TextColor = ThemeColors.TextPrimary;
        _scoreLabel.HorizontalOptions = LayoutOptions.Center;

        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12,
            HorizontalOptions = LayoutOptions.Center,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(100) },
                new ColumnDefinition { Width = new GridLength(100) }
            },
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(100) },
                new RowDefinition { Height = new GridLength(100) }
            }
        };

        for (var i = 0; i < Buttons.Length; i++)
        {
            var idx = i;
            var btn = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Stroke = Colors.Transparent,
                BackgroundColor = Buttons[i].Color.WithAlpha(0.6f),
                WidthRequest = 100,
                HeightRequest = 100
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnButtonTapped(idx);
            btn.GestureRecognizers.Add(tap);

            Grid.SetRow(btn, i / 2);
            Grid.SetColumn(btn, i % 2);
            grid.Children.Add(btn);
            _buttonViews.Add(btn);
        }

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = Definition.Title, FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = BrainSkillInfo.Accent(Definition.Skill),
                    HorizontalOptions = LayoutOptions.Center
                },
                _statusLabel,
                _scoreLabel,
                grid
            }
        };
    }

    private async Task StartRoundAsync()
    {
        _showing = true;
        _playerIndex = 0;

        _sequence.Add(_rng.Next(Buttons.Length));

        _statusLabel.Text = $"Раунд {_round + 1} из {Rounds} · Запоминай";
        _scoreLabel.Text = $"Очки: {_score}";

        await Task.Delay(500);

        for (var i = 0; i < _sequence.Count; i++)
        {
            var idx = _sequence[i];
            _buttonViews[idx].BackgroundColor = Buttons[idx].Color;
            _buttonViews[idx].Opacity = 1.0;
            await Task.Delay(400);
            _buttonViews[idx].BackgroundColor = Buttons[idx].Color.WithAlpha(0.6f);
            _buttonViews[idx].Opacity = 1.0;
            await Task.Delay(150);
        }

        _statusLabel.Text = $"Раунд {_round + 1} из {Rounds} · Повторяй!";
        _showing = false;
    }

    private async void OnButtonTapped(int index)
    {
        if (_showing) return;

        _buttonViews[index].BackgroundColor = Buttons[index].Color;
        _buttonViews[index].Opacity = 1.0;
        _ = _buttonViews[index].PopAsync();
        await Task.Delay(200);
        _buttonViews[index].BackgroundColor = Buttons[index].Color.WithAlpha(0.6f);

        if (index != _sequence[_playerIndex])
        {
            HapticService.Error();
            SoundService.PlayWrong();
            _statusLabel.Text = "Ошибка!";
            await Task.Delay(600);
            await FinishAsync(_score, Rounds * (Rounds + 1) / 2);
            return;
        }

        HapticService.Click();
        SoundService.PlayCorrect();
        _playerIndex++;
        _score++;

        if (_playerIndex >= _sequence.Count)
        {
            _round++;
            _scoreLabel.Text = $"Очки: {_score}";

            if (_round >= Rounds)
            {
                await FinishAsync(_score, Rounds * (Rounds + 1) / 2);
                return;
            }

            await Task.Delay(400);
            _ = StartRoundAsync();
        }
    }
}
