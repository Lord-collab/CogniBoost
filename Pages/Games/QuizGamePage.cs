using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

public sealed record QuizQuestion(string Prompt, string[] Options, int CorrectIndex);

/// <summary>
/// Абстрактный базовый класс для квиз-игр (вопрос-варианты ответа).
/// Предоставляет: таймер на вопрос (20 с), прогресс-бар, блокировку
/// повторного нажатия, подсчёт точности.
///
/// Наследники (MatrixLogicGamePage, OddWordGamePage, WordChainGamePage)
/// только передают список вопросов в конструктор.
/// </summary>
public abstract class QuizGamePage : GameBasePage
{
    private const int TimePerQuestion = 20;

    private readonly List<QuizQuestion> _questions;
    private readonly Label _progressLabel = new();
    private readonly Label _promptLabel = new();
    private readonly Label _timerLabel = new();
    private readonly VerticalStackLayout _optionsLayout = new() { Spacing = 12 };
    private readonly ProgressBar _timerBar = new()
    {
        Progress = 1.0,
        HeightRequest = 6,
        BackgroundColor = ThemeColors.Divider
    };

    private int _index;
    private int _correct;
    private bool _locked;
    private IDispatcherTimer? _timer;
    private int _timeLeft;

    protected QuizGamePage(GameDefinition definition, IEnumerable<QuizQuestion> questions)
        : base(definition)
    {
        _questions = questions.ToList();
        BuildUi();
        ShowQuestion();
    }

    protected Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;

        _timerLabel.FontSize = 14;
        _timerLabel.TextColor = ThemeColors.Accent;
        _timerLabel.FontAttributes = FontAttributes.Bold;
        _timerLabel.HorizontalOptions = LayoutOptions.End;

        _timerBar.ProgressColor = ThemeColors.Accent;

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { _progressLabel }
        };
        Grid.SetColumn(_timerLabel, 1);
        header.Children.Add(_timerLabel);

        _promptLabel.FontSize = 24;
        _promptLabel.FontAttributes = FontAttributes.Bold;
        _promptLabel.HorizontalTextAlignment = TextAlignment.Center;
        _promptLabel.TextColor = ThemeColors.TextPrimary;

        var promptCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 24,
            Content = _promptLabel
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = Definition.Title,
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Accent
                    },
                    header,
                    _timerBar,
                    promptCard,
                    _optionsLayout
                }
            }
        };
    }

    private void ShowQuestion()
    {
        _locked = false;
        _timeLeft = TimePerQuestion;
        _timerLabel.Text = $"⏱ {_timeLeft}";
        _timerBar.ProgressColor = ThemeColors.Accent;
        _timerBar.Progress = 1.0;

        var question = _questions[_index];

        _progressLabel.Text = $"Вопрос {_index + 1} из {_questions.Count}";
        _promptLabel.Text = question.Prompt;

        _optionsLayout.Children.Clear();
        for (var i = 0; i < question.Options.Length; i++)
        {
            var optionIndex = i;
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Stroke = ThemeColors.Border,
                StrokeThickness = 1,
                BackgroundColor = ThemeColors.CardBg,
                Padding = new Thickness(16, 14),
                Content = new Label
                {
                    Text = question.Options[i],
                    TextColor = ThemeColors.TextPrimary,
                    FontSize = 17
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnAnswer(optionIndex, border);
            border.GestureRecognizers.Add(tap);
            _optionsLayout.Children.Add(border);
        }

        StartTimer();
    }

    private void StartTimer()
    {
        _timer?.Stop();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            _timeLeft--;
            _timerLabel.Text = $"⏱ {_timeLeft}";
            _timerBar.Progress = (double)_timeLeft / TimePerQuestion;

            if (_timeLeft <= 5)
                _timerBar.ProgressColor = ThemeColors.Error;
            else if (_timeLeft <= 10)
                _timerBar.ProgressColor = ThemeColors.Warning;

            if (_timeLeft <= 0)
            {
                _timer?.Stop();
                _locked = true;
                // Timeout — show the correct answer
                var question = _questions[_index];
                HapticService.Error();
                SoundService.PlayWrong();
                if (_optionsLayout.Children.ElementAtOrDefault(question.CorrectIndex) is Border correctBorderT
                    && correctBorderT.Content is Label correctLabelT)
                {
                    correctBorderT.BackgroundColor = ThemeColors.Success;
                    correctLabelT.TextColor = Colors.White;
                }
                _ = NextQuestion();
            }
        };
        _timer.Start();
    }

    private async void OnAnswer(int chosenIndex, Border border)
    {
        if (_locked) return;
        _locked = true;
        _timer?.Stop();

        var question = _questions[_index];
        var isCorrect = chosenIndex == question.CorrectIndex;

        if (isCorrect)
        {
            _correct++;
            border.BackgroundColor = Accent;
            if (border.Content is Label l) l.TextColor = Colors.White;
            HapticService.Click();
            SoundService.PlayCorrect();
            _ = border.PopAsync();
        }
        else
        {
            border.BackgroundColor = ThemeColors.Error;
            if (border.Content is Label l2) l2.TextColor = Colors.White;
            HapticService.Error();
            SoundService.PlayWrong();
            _ = border.ShakeAsync();

            if (_optionsLayout.Children.ElementAtOrDefault(question.CorrectIndex) is Border correctBorder
                && correctBorder.Content is Label correctLabel)
            {
                correctBorder.BackgroundColor = ThemeColors.Success;
                correctLabel.TextColor = Colors.White;
            }
        }

        await Task.Delay(700);
        _ = NextQuestion();
    }

    private async Task NextQuestion()
    {
        _index++;
        if (_index >= _questions.Count)
        {
            await FinishAsync(_correct, _questions.Count);
        }
        else
        {
            ShowQuestion();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }
}
