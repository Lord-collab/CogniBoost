using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>
/// Экран результата теста: условный IQ-балл, точность и начисленные бонусы.
/// </summary>
public sealed class TestResultPage : ContentPage
{
    public TestResultPage(TestResult result)
    {
        Title = "Результат теста";
        BackgroundColor = ThemeColors.PageBg;
        Shell.SetNavBarIsVisible(this, false);
        NavigationPage.SetHasNavigationBar(this, false);

        var accent = Color.FromArgb("#7C3AED");

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
                    new Label { Text = "🧠", FontSize = 60, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Твой IQ", FontSize = 16, HorizontalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#6B7280") },
                    new Label { Text = result.IqScore.ToString(), FontSize = 56, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, TextColor = accent },
                    new Label { Text = TestBank.IqBand(result.IqScore), FontSize = 18, HorizontalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#1A1A2E") },
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB"), Margin = new Thickness(0, 8) },
                    BuildStatRow("Правильных ответов", $"{result.CorrectAnswers} из {result.TotalQuestions}"),
                    BuildStatRow("Точность", $"{result.AccuracyPercent}%"),
                    BuildStatRow("Начислено бонусов", $"+{result.EarnedPoints} ⭐", accent),
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
        Grid.SetRow(bottom, 1);

        Content = new Grid
        {
            Padding = 24,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children = { top, bottom }
        };
    }

    private static View BuildStatRow(string label, string value, Color? valueColor = null)
    {
        var labelView = new Label { Text = label, FontSize = 15, TextColor = Color.FromArgb("#6B7280"), VerticalOptions = LayoutOptions.Center };
        var valueView = new Label { Text = value, FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = valueColor ?? Color.FromArgb("#1A1A2E"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(valueView, 1);

        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { labelView, valueView }
        };
    }

    private async Task ShareAsync(TestResult result)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Мой результат в CogniBoost",
            Text = $"Мой IQ в CogniBoost: {result.IqScore} ({TestBank.IqBand(result.IqScore)})! " +
                   $"Правильных ответов: {result.CorrectAnswers} из {result.TotalQuestions}."
        });
    }
}
