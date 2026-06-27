using CogniBoost.Models;
using CogniBoost.Pages.Controls;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public partial class TestsPage : ContentPage
{
    public TestsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var history = await ProgressStore.GetTestHistoryAsync();
        var bestIq = history.Count > 0 ? history.Max(t => t.IqScore) : 0;

        BestIqLabel.Text = bestIq > 0 ? bestIq.ToString() : "—";
        TestsCountLabel.Text = history.Count switch
        {
            0 => "Пройди первый тест",
            1 => "1 тест пройден",
            _ => $"{history.Count} тестов пройдено"
        };

        var rank = TestBank.IqBand(bestIq);
        RankLabel.Text = history.Count > 0 ? rank : "Пройди первый тест";

        if (history.Count > 0)
        {
            var best = history.OrderByDescending(t => t.IqScore).First();
            BestDateLabel.Text = best.PlayedAtUtc.ToLocalTime().ToString("dd.MM.yy");
        }
        else
        {
            BestDateLabel.Text = "";
        }

        BuildTests();
        BuildHistory(history);
    }

    private void BuildTests()
    {
        TestsLayout.Children.Clear();

        foreach (var test in TestBank.All)
        {
            var questions = test.BuildQuestions();
            var questionCount = questions.Count(q => q.Prompt != "Далее" && !q.Prompt.StartsWith("Запомни слова"));
            if (questionCount == 0) questionCount = questions.Count;

            var emoji = test.Id switch
            {
                "iq_express" => "🧠",
                "memory_words" => "📖",
                "focus_test" => "🎯",
                "logic_test" => "⚖️",
                "numerical_test" => "🔢",
                _ => "📝"
            };
            var iconColor = test.Id switch
            {
                "iq_express" => ThemeColors.Accent,
                "memory_words" => Color.FromArgb("#FF6B9D"),
                "focus_test" => Color.FromArgb("#FFA726"),
                "logic_test" => Color.FromArgb("#AB47BC"),
                "numerical_test" => Color.FromArgb("#42A5F5"),
                _ => ThemeColors.Success
            };

            var emojiLabel = new Label
            {
                Text = emoji,
                FontSize = 32,
                VerticalOptions = LayoutOptions.Start,
                WidthRequest = 48,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var titleLabel = new Label
            {
                Text = test.Title,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColors.TextPrimary
            };

            var metaLabel = new Label
            {
                Text = $"{questionCount} вопросов",
                FontSize = 12,
                TextColor = ThemeColors.TextMuted
            };

            var descLabel = new Label
            {
                Text = test.Description,
                FontSize = 12,
                TextColor = ThemeColors.TextSecondary,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2
            };

            var startBtn = new Button
            {
                Text = "Начать",
                BackgroundColor = iconColor,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                HeightRequest = 36,
                CornerRadius = 10,
                Padding = new Thickness(16, 0),
                VerticalOptions = LayoutOptions.Center
            };
            startBtn.Clicked += async (_, _) =>
                await Navigation.PushAsync(new TestSessionPage(test.Id));

            var topRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12
            };
            topRow.Children.Add(emojiLabel);

            var textStack = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children = { titleLabel, metaLabel }
            };
            Grid.SetColumn(textStack, 1);
            topRow.Children.Add(textStack);
            Grid.SetColumn(startBtn, 2);
            topRow.Children.Add(startBtn);

            TestsLayout.Children.Add(new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Stroke = Colors.Transparent,
                BackgroundColor = ThemeColors.CardBg,
                Padding = new Thickness(14),
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Offset = new Point(0, 1), Radius = 6, Opacity = 0.04f
                },
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children = { topRow, descLabel }
                }
            });
        }
    }

    private void BuildHistory(List<TestResult> history)
    {
        HistoryLayout.Children.Clear();

        if (history.Count == 0)
        {
            HistoryLayout.Children.Add(new EmptyState("📝",
                "История пуста", "Пройди тест, чтобы увидеть результаты!", ""));
            return;
        }

        foreach (var r in history.Take(10))
        {
            var iqColor = r.IqScore switch
            {
                >= 120 => ThemeColors.Success,
                >= 100 => ThemeColors.Accent,
                _ => ThemeColors.TextMuted
            };

            var leftStack = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = r.TestTitle,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColors.TextPrimary
                    },
                    new Label
                    {
                        Text = r.PlayedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                        FontSize = 11,
                        TextColor = ThemeColors.TextMuted
                    }
                }
            };

            var rightStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
                Spacing = 1,
                Children =
                {
                    new Label
                    {
                        Text = $"IQ {r.IqScore}",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = iqColor,
                        HorizontalOptions = LayoutOptions.End
                    },
                    new Label
                    {
                        Text = $"{r.AccuracyPercent}% верно",
                        FontSize = 12,
                        TextColor = ThemeColors.TextMuted,
                        HorizontalOptions = LayoutOptions.End
                    }
                }
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children = { leftStack }
            };
            Grid.SetColumn(rightStack, 1);
            grid.Children.Add(rightStack);

            HistoryLayout.Children.Add(new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Stroke = Colors.Transparent,
                BackgroundColor = ThemeColors.CardBg,
                Padding = new Thickness(14),
                Content = grid
            });
        }
    }
}
