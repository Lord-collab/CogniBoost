using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Цветовая память»: запомни порядок цветных кружков и повтори его.
/// С каждым раундом последовательность растёт на один элемент.
/// </summary>
public sealed class ColorSequenceGamePage : GameBasePage
{
    private const int Rounds = 5;
    private const int StartLength = 3;

    private static readonly (string Name, Color Color)[] Palette =
    {
        ("Красный",    Color.FromArgb("#FF5370")),
        ("Синий",      Color.FromArgb("#29B6F6")),
        ("Зелёный",    Color.FromArgb("#26D982")),
        ("Жёлтый",     Color.FromArgb("#FFB347")),
        ("Фиолетовый", Color.FromArgb("#9B59F5")),
        ("Розовый",    Color.FromArgb("#F06292")),
    };

    private readonly Label _statusLabel = new();
    private readonly Label _promptLabel = new();
    private readonly Grid _displayGrid = new();
    private readonly FlexLayout _inputLayout = new();
    private readonly Random _rng = new();

    private int   _round;
    private int   _correct;
    private int[] _sequence = Array.Empty<int>();
    private int   _inputPos;
    private bool  _showPhase;

    public ColorSequenceGamePage()
        : base(GameCatalog.Get("color_sequence")!)
    {
        BuildUi();
        _ = StartRoundAsync();
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _statusLabel.FontSize    = 14;
        _statusLabel.TextColor   = ThemeColors.TextMuted;

        _promptLabel.FontSize    = 18;
        _promptLabel.FontAttributes = FontAttributes.Bold;
        _promptLabel.HorizontalOptions = LayoutOptions.Center;
        _promptLabel.TextColor   = ThemeColors.TextPrimary;

        // Дисплей: одна большая цветная плашка
        _displayGrid.HeightRequest = 140;
        _displayGrid.BackgroundColor = ThemeColors.Divider;

        // Кнопки ввода
        _inputLayout.Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap;
        _inputLayout.JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Center;
        _inputLayout.Margin = new Thickness(0, 12, 0, 0);
        RebuildInputButtons();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24), Spacing = 14,
                Children =
                {
                    new Label { Text = Definition.Title, FontSize = 22,
                        FontAttributes = FontAttributes.Bold, TextColor = Accent },
                    _statusLabel,
                    new Border
                    {
                        StrokeShape = new RoundRectangle { CornerRadius = 20 },
                        Stroke = Colors.Transparent,
                        BackgroundColor = ThemeColors.Divider,
                        HeightRequest = 140,
                        Content = _promptLabel
                    },
                    _inputLayout
                }
            }
        };
    }

    private void RebuildInputButtons()
    {
        _inputLayout.Children.Clear();
        foreach (var (name, color) in Palette)
        {
            var btn = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 36 },
                Stroke = Colors.Transparent,
                BackgroundColor = color,
                WidthRequest = 64, HeightRequest = 64,
                Margin = new Thickness(6),
                Content = new Label
                {
                    Text = name[0].ToString(),
                    FontSize = 22, FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            var colorCopy = color;
            var nameCopy  = name;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnColorTapped(colorCopy, nameCopy);
            btn.GestureRecognizers.Add(tap);
            _inputLayout.Children.Add(btn);
        }
    }

    private async Task StartRoundAsync()
    {
        _showPhase = true;
        var length = StartLength + _round;
        _sequence  = Enumerable.Range(0, length).Select(_ => _rng.Next(Palette.Length)).ToArray();
        _inputPos  = 0;

        _statusLabel.Text = $"Раунд {_round + 1} из {Rounds} · запоминай";
        SetInputEnabled(false);

        // Показываем последовательность
        for (var i = 0; i < _sequence.Length; i++)
        {
            var idx = _sequence[i];
            _promptLabel.Text      = Palette[idx].Name;
            _promptLabel.TextColor = Palette[idx].Color;
            await Task.Delay(700);
            _promptLabel.Text      = "·";
            _promptLabel.TextColor = ThemeColors.TextMuted;
            await Task.Delay(300);
        }
 
         _promptLabel.Text      = "Повтори!";
         _promptLabel.TextColor = ThemeColors.TextPrimary;
        _statusLabel.Text      = $"Раунд {_round + 1} из {Rounds} · повторяй";
        _showPhase = false;
        SetInputEnabled(true);
    }

    private async void OnColorTapped(Color color, string name)
    {
        if (_showPhase || _inputPos >= _sequence.Length) return;

        var expected = _sequence[_inputPos];
        _promptLabel.Text      = name;
        _promptLabel.TextColor = color;

        if (Array.IndexOf(Palette, Palette.FirstOrDefault(p => p.Color == color)) == expected)
        {
            _inputPos++;
            HapticService.Click();
            SoundService.PlayCorrect();
            if (_inputPos >= _sequence.Length)
            {
                _correct++;
                await Task.Delay(400);
                _round++;
                if (_round >= Rounds) { await FinishAsync(_correct, Rounds); return; }
                await StartRoundAsync();
            }
        }
        else
        {
            HapticService.Error();
            SoundService.PlayWrong();
            _promptLabel.TextColor = ThemeColors.Error;
            await Task.Delay(700);
            _round++;
            if (_round >= Rounds) { await FinishAsync(_correct, Rounds); return; }
            await StartRoundAsync();
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (var child in _inputLayout.Children.OfType<Border>())
            child.Opacity = enabled ? 1.0 : 0.4;
    }
}
