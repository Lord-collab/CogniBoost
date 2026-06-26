using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Запомни число»: показывается последовательность цифр, затем её нужно
/// ввести по памяти. С каждым раундом длина растёт.
/// </summary>
public sealed class NumberRecallGamePage : GameBasePage
{
    private const int Rounds = 5;
    private const int StartLength = 3;

    private readonly Label _progressLabel = new();
    private readonly Label _sequenceLabel = new();
    private readonly Entry _answerEntry = new();
    private readonly Button _actionButton = new();
    private readonly Random _rng = new();

    private int _round;
    private int _correct;
    private string _currentSequence = string.Empty;
    private bool _inputPhase;

    public NumberRecallGamePage()
        : base(GameCatalog.Get("number_recall")!)
    {
        BuildUi();
        _ = StartRoundAsync();
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;
 
         _sequenceLabel.FontSize = 44;
         _sequenceLabel.FontAttributes = FontAttributes.Bold;
         _sequenceLabel.HorizontalOptions = LayoutOptions.Center;
         _sequenceLabel.TextColor = ThemeColors.TextPrimary;

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 32,
            HeightRequest = 140,
            Content = _sequenceLabel
        };

        _answerEntry.Placeholder = "Введи число";
        _answerEntry.Keyboard = Keyboard.Numeric;
        _answerEntry.FontSize = 22;
        _answerEntry.HorizontalTextAlignment = TextAlignment.Center;
        _answerEntry.IsVisible = false;

        _actionButton.Text = "Запомни…";
        _actionButton.BackgroundColor = Accent;
        _actionButton.TextColor = Colors.White;
        _actionButton.FontAttributes = FontAttributes.Bold;
        _actionButton.HeightRequest = 52;
        _actionButton.CornerRadius = 14;
        _actionButton.IsEnabled = false;
        _actionButton.Clicked += OnActionClicked;

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 20,
            Children =
            {
                _progressLabel,
                new Label
                {
                    Text = Definition.Title,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Accent
                },
                card,
                _answerEntry,
                _actionButton
            }
        };
    }

    private async Task StartRoundAsync()
    {
        _inputPhase = false;
        _progressLabel.Text = $"Раунд {_round + 1} из {Rounds}";
        _answerEntry.IsVisible = false;
        _answerEntry.Text = string.Empty;
        _actionButton.IsEnabled = false;
        _actionButton.Text = "Запоминай…";

        var length = StartLength + _round;
        _currentSequence = string.Concat(Enumerable.Range(0, length).Select(_ => _rng.Next(0, 10)));
        _sequenceLabel.Text = _currentSequence;

        // Время показа растёт с длиной.
        await Task.Delay(1200 + length * 400);

        // Прячем последовательность, переходим к вводу.
        _sequenceLabel.Text = "🔒";
        _answerEntry.IsVisible = true;
        _actionButton.IsEnabled = true;
        _actionButton.Text = "Проверить";
        _inputPhase = true;
    }

    private async void OnActionClicked(object? sender, EventArgs e)
    {
        if (!_inputPhase)
        {
            return;
        }

        _inputPhase = false;

        if (string.Equals(_answerEntry.Text?.Trim(), _currentSequence, StringComparison.Ordinal))
        {
            _correct++;
            HapticService.Click();
            SoundService.PlayCorrect();
        }
        else
        {
            HapticService.Error();
            SoundService.PlayWrong();
        }

        _round++;
        if (_round >= Rounds)
        {
            await FinishAsync(_correct, Rounds);
        }
        else
        {
            await StartRoundAsync();
        }
    }
}
