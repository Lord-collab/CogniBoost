using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Истинный цвет» (тест Струпа): на экране слово-название цвета, окрашенное
/// в другой цвет. Нужно выбрать ЦВЕТ ШРИФТА, игнорируя значение слова.
/// </summary>
public sealed class StroopGamePage : GameBasePage
{
    private const int Rounds = 10;

    private static readonly (string Name, Color Color)[] Palette =
    {
        ("КРАСНЫЙ", Color.FromArgb("#EF4444")),
        ("СИНИЙ", Color.FromArgb("#3B82F6")),
        ("ЗЕЛЁНЫЙ", Color.FromArgb("#10B981")),
        ("ЖЁЛТЫЙ", Color.FromArgb("#F59E0B")),
        ("ФИОЛЕТОВЫЙ", Color.FromArgb("#7C3AED")),
    };

    private readonly Label _progressLabel = new();
    private readonly Label _wordLabel = new();
    private readonly VerticalStackLayout _optionsLayout = new() { Spacing = 12 };
    private readonly Random _rng = new();

    private int _index;
    private int _correct;
    private int _inkColorIndex;
    private bool _locked;

    public StroopGamePage()
        : base(GameCatalog.Get("stroop_color")!)
    {
        BuildUi();
        ShowRound();
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;

        _wordLabel.FontSize = 48;
        _wordLabel.FontAttributes = FontAttributes.Bold;
        _wordLabel.HorizontalOptions = LayoutOptions.Center;

        var wordCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 32,
            Content = _wordLabel
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 20,
                Children =
                {
                    _progressLabel,
                    new Label
                    {
                        Text = "Выбери ЦВЕТ шрифта, а не слово",
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Accent
                    },
                    wordCard,
                    _optionsLayout
                }
            }
        };
    }

    private void ShowRound()
    {
        _locked = false;
        _progressLabel.Text = $"Раунд {_index + 1} из {Rounds}";

        // Слово (значение) и цвет шрифта выбираются независимо.
        var wordIndex = _rng.Next(Palette.Length);
        _inkColorIndex = _rng.Next(Palette.Length);

        _wordLabel.Text = Palette[wordIndex].Name;
        _wordLabel.TextColor = Palette[_inkColorIndex].Color;

        _optionsLayout.Children.Clear();
        for (var i = 0; i < Palette.Length; i++)
        {
            var colorIndex = i;
            var button = new Button
            {
                Text = Palette[i].Name,
                BackgroundColor = ThemeColors.CardBg,
                TextColor = ThemeColors.TextPrimary,
                FontSize = 17,
                HeightRequest = 50,
                CornerRadius = 14,
                BorderColor = ThemeColors.Border,
                BorderWidth = 1
            };
            button.Clicked += (_, _) => OnAnswer(colorIndex, button);
            _optionsLayout.Children.Add(button);
        }
    }

    private async void OnAnswer(int chosenColorIndex, Button button)
    {
        if (_locked)
        {
            return;
        }

        _locked = true;

        if (chosenColorIndex == _inkColorIndex)
        {
            _correct++;
            button.BackgroundColor = ThemeColors.Success;
            button.TextColor = Colors.White;
            HapticService.Click();
            SoundService.PlayCorrect();
            _ = button.PopAsync();
        }
        else
        {
            button.BackgroundColor = ThemeColors.Error;
            button.TextColor = Colors.White;
            HapticService.Error();
            SoundService.PlayWrong();
            _ = button.ShakeAsync();
        }

        await Task.Delay(600);

        _index++;
        if (_index >= Rounds)
        {
            await FinishAsync(_correct, Rounds);
        }
        else
        {
            ShowRound();
        }
    }
}
