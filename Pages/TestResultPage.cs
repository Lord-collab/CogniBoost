using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>Экран результата теста: IQ, баллы, точность, кнопки поделиться/повтора.</summary>
public sealed class TestResultPage : ContentPage
{
    public TestResultPage(TestResult result, IReadOnlyList<Achievement>? newAchievements = null)
    {
        Title = "Результат теста";
        BackgroundColor = ThemeColors.PageBg;
        Shell.SetNavBarIsVisible(this, false);
        NavigationPage.SetHasNavigationBar(this, false);

        var accent = ThemeColors.Tertiary;

        // Если есть новые ачивки — показываем их в карточке
        var statsContent = new VerticalStackLayout { Spacing = 12 };
        statsContent.Children.Add(BuildStatRow("Правильных ответов", $"{result.CorrectAnswers} из {result.TotalQuestions}"));
        statsContent.Children.Add(BuildStatRow("Точность", $"{result.AccuracyPercent}%"));
        statsContent.Children.Add(BuildStatRow("Начислено бонусов", $"+{result.EarnedPoints} ⭐", accent));

        if (newAchievements?.Count > 0)
        {
            statsContent.Children.Add(new BoxView
            {
                HeightRequest = 1, Color = ThemeColors.Divider,
                Margin = new Thickness(0, 4)
            });
            foreach (var ach in newAchievements)
            {
                statsContent.Children.Add(new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    Stroke = Colors.Transparent,
                    BackgroundColor = ThemeColors.Accent.WithAlpha(0.12f),
                    Padding = new Thickness(12, 8),
                    Content = new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = ach.Emoji, FontSize = 24, VerticalOptions = LayoutOptions.Center },
                            new VerticalStackLayout
                            {
                                VerticalOptions = LayoutOptions.Center, Spacing = 1,
                                Children =
                                {
                                    new Label { Text = "Новое достижение!", FontSize = 11, TextColor = ThemeColors.Accent },
                                    new Label { Text = ach.Title, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = ThemeColors.TextPrimary }
                                }
                            }
                        }
                    }
                });
            }
        }

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 28,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "🧠", FontSize = 60,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Твой IQ", FontSize = 16,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = ThemeColors.TextMuted
                    },
                    new Label
                    {
                        Text = result.IqScore.ToString(), FontSize = 56,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = accent
                    },
                    new Label
                    {
                        Text = TestBank.IqBand(result.IqScore), FontSize = 18,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = ThemeColors.TextPrimary
                    },
                    statsContent
                }
            }
        };

        var shareButton = new Button
        {
            Text = "Поделиться результатом",
            BackgroundColor = accent,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 52,
            CornerRadius = 14
        };
        shareButton.Clicked += async (_, _) => await ShareAsync(result);

        var doneButton = new Button
        {
            Text = "Готово",
            BackgroundColor = Colors.Transparent,
            TextColor = accent,
            BorderColor = accent,
            BorderWidth = 1,
            HeightRequest = 52,
            CornerRadius = 14
        };
        doneButton.Clicked += async (_, _) => await Navigation.PopToRootAsync();

        var top = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { card } };
        var bottom = new VerticalStackLayout { Spacing = 12, Children = { shareButton, doneButton } };
        var grid = new Grid
        {
            Padding = 24,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children = { top }
        };
        Grid.SetRow(bottom, 1);
        grid.Children.Add(bottom);
        Content = grid;
    }

    private static View BuildStatRow(string label, string value, Color? valueColor = null)
    {
        var labelView = new Label
        {
            Text = label, FontSize = 15,
            TextColor = ThemeColors.TextMuted,
            VerticalOptions = LayoutOptions.Center
        };
        var valueView = new Label
        {
            Text = value, FontSize = 17, FontAttributes = FontAttributes.Bold,
            TextColor = valueColor ?? ThemeColors.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        };
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { labelView }
        };
        Grid.SetColumn(valueView, 1);
        grid.Children.Add(valueView);
        return grid;
    }

    private async Task ShareAsync(TestResult result)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Мой результат в CogniBoost",
            Text = $"Мой IQ в CogniBoost: {result.IqScore} ({TestBank.IqBand(result.IqScore)})! " +
                   $"Правильных ответов: {result.CorrectAnswers} из {result.TotalQuestions}.\n\n" +
                   $"Скачать: {SettingsService.AppDownloadUrl}"
        });
    }
}
