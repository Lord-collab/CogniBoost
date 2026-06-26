using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>
/// Прохождение теста: вопросы с вариантами ответа и общий таймер.
/// По истечении времени или после последнего вопроса показывается результат.
/// </summary>
public sealed class TestSessionPage : ContentPage
{
    private readonly TestDefinition _test;
    private readonly IReadOnlyList<TestQuestion> _questions;

    private readonly Label _timerLabel = new();
    private readonly Label _progressLabel = new();
    private readonly Label _promptLabel = new();
    private readonly VerticalStackLayout _optionsLayout = new() { Spacing = 12 };

    private IDispatcherTimer? _timer;
    private int _remainingSeconds;
    private int _index;
    private int _correct;
    private bool _locked;
    private bool _finished;

    public TestSessionPage(string testId)
    {
        _test = TestBank.Get(testId) ?? TestBank.All[0];
        _questions = _test.BuildQuestions();
        _remainingSeconds = _test.DurationSeconds;

        Title = _test.Title;
        BackgroundColor = ThemeColors.PageBg;

        BuildUi();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartTimer();
        ShowQuestion();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    private void BuildUi()
    {
        _timerLabel.FontSize = 18;
        _timerLabel.FontAttributes = FontAttributes.Bold;
        _timerLabel.TextColor = ThemeColors.Tertiary;
        _timerLabel.HorizontalOptions = LayoutOptions.End;

        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;
        _progressLabel.VerticalOptions = LayoutOptions.Center;

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

        _promptLabel.FontSize = 22;
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
                Spacing = 18,
                Children = { header, promptCard, _optionsLayout }
            }
        };
    }

    private void StartTimer()
    {
        UpdateTimerLabel();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, _) =>
        {
            _remainingSeconds--;
            UpdateTimerLabel();

            if (_remainingSeconds <= 0)
            {
                _timer?.Stop();
                await FinishAsync();
            }
        };
        _timer.Start();
    }

    private void UpdateTimerLabel()
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
        _timerLabel.Text = $"⏱ {span:mm\\:ss}";
    }

    private void ShowQuestion()
    {
        _locked = false;
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
            tap.Tapped += (_, _) => OnAnswer(optionIndex);
            border.GestureRecognizers.Add(tap);
            _optionsLayout.Children.Add(border);
        }
    }

    private void OnAnswer(int chosenIndex)
    {
        if (_locked)
        {
            return;
        }

        _locked = true;
        if (chosenIndex == _questions[_index].CorrectIndex)
        {
            _correct++;
        }

        _index++;
        if (_index >= _questions.Count)
        {
            _ = FinishAsync();
        }
        else
        {
            ShowQuestion();
        }
    }

    private async Task FinishAsync()
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        _timer?.Stop();

        var iq = TestBank.CalculateIq(_correct, _questions.Count);
        var accuracy = _questions.Count > 0 ? _correct / (double)_questions.Count : 0;
        var earned = PointsService.AwardForResult(accuracy);

        var result = new TestResult(
            TestId: _test.Id,
            TestTitle: _test.Title,
            CorrectAnswers: _correct,
            TotalQuestions: _questions.Count,
            IqScore: iq,
            EarnedPoints: earned,
            PlayedAtUtc: DateTime.UtcNow);

        ProgressStore.AddTestResult(result);

        // Streak + достижения
        StreakService.RecordActivity();
        var newAchievements = AchievementsService.CheckAndUnlock();

        // Попап новых достижений
        if (newAchievements.Count > 0)
            await AchievementPopupService.ShowAsync(this, newAchievements);

        // Синхронизация в фоне
        _ = CloudSyncService.SyncTestResultAsync(result);
        _ = CloudSyncService.SyncCurrentUserAsync();

        var resultPage = new TestResultPage(result, newAchievements);
        await Navigation.PushAsync(resultPage);
        Navigation.RemovePage(this);
    }
}
