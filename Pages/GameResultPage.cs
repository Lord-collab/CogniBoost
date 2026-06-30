using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>Экран результата игры: счёт, точность, бонусы, стрик, кнопки поделиться/повтора.</summary>
public class GameResultPage : ContentPage
{
    public GameResultPage(
        GameResult result,
        int streak = 0,
        int streakBonus = 0,
        IReadOnlyList<Achievement>? newAchievements = null)
    {
        Title = "Результат";
        BackgroundColor = ThemeColors.PageBg;
        Shell.SetNavBarIsVisible(this, false);
        NavigationPage.SetHasNavigationBar(this, false);

        var accent = BrainSkillInfo.Accent(result.Skill);

        var statsRows = new VerticalStackLayout { Spacing = 10 };
        statsRows.Children.Add(BuildStatRow("Счёт",     $"{result.Score} из {result.MaxScore}"));
        statsRows.Children.Add(BuildStatRow("Точность", $"{result.AccuracyPercent}%"));
        statsRows.Children.Add(BuildStatRow("Бонусы",   $"+{result.EarnedPoints} ⭐", accent));

        var isChallenge = DailyChallengeService.IsChallengeGame(result.GameId);
        if (isChallenge)
            statsRows.Children.Add(BuildStatRow("🏆 Челлендж", "×2 бонусы",
                ThemeColors.Success));

        if (streakBonus > 0)
            statsRows.Children.Add(BuildStatRow($"Бонус серии 🔥{streak}", $"+{streakBonus} ⭐",
                ThemeColors.Warning));

        if (newAchievements?.Count > 0)
        {
            statsRows.Children.Add(new BoxView
            {
                HeightRequest = 1, Color = ThemeColors.Divider,
                Margin = new Thickness(0, 6)
            });
            foreach (var ach in newAchievements)
            {
                statsRows.Children.Add(new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = Colors.Transparent,
                    BackgroundColor = ThemeColors.Accent.WithAlpha(0.12f),
                    Padding = new Thickness(12, 8),
                    Content = new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label
                            {
                                Text = ach.Emoji, FontSize = 24,
                                VerticalOptions = LayoutOptions.Center
                            },
                            new VerticalStackLayout
                            {
                                VerticalOptions = LayoutOptions.Center,
                                Spacing = 1,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = "Новое достижение!", FontSize = 11,
                                        TextColor = ThemeColors.Accent
                                    },
                                    new Label
                                    {
                                        Text = ach.Title, FontSize = 14,
                                        FontAttributes = FontAttributes.Bold,
                                        TextColor = ThemeColors.TextPrimary
                                    }
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
            Padding = 24,
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = result.AccuracyPercent >= 70 ? "🎉" : "💪",
                        FontSize = 56, HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = result.GameTitle, FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = ThemeColors.TextPrimary
                    },
                    statsRows
                }
            }
        };

        var playAgain = new Button
        {
            Text = "Играть снова", BackgroundColor = accent,
            TextColor = Colors.White, FontAttributes = FontAttributes.Bold,
            HeightRequest = 52, CornerRadius = 14
        };
        playAgain.Clicked += async (_, _) => await RestartAsync(result.GameId);

        var shareButton = new Button
        {
            Text = "Поделиться",
            BackgroundColor = Colors.Transparent,
            TextColor = accent,
            BorderColor = accent,
            BorderWidth = 1.5,
            HeightRequest = 52,
            CornerRadius = 14
        };
        shareButton.Clicked += async (_, _) => await ShareResult(result);

        var toGames = new Button
        {
            Text = "К списку игр", BackgroundColor = Colors.Transparent,
            TextColor = accent, BorderColor = accent, BorderWidth = 1.5,
            HeightRequest = 52, CornerRadius = 14
        };
        toGames.Clicked += async (_, _) => await Navigation.PopToRootAsync();

        var bottom = new VerticalStackLayout { Spacing = 12, Children = { playAgain, shareButton, toGames } };
        Grid.SetRow(bottom, 1);

        Content = new Grid
        {
            Padding = 24,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Children = { card }
                    }
                },
                bottom
            }
        };
    }

    private static View BuildStatRow(string label, string value, Color? valueColor = null)
    {
        var lbl = new Label
        {
            Text = label, FontSize = 15,
            TextColor = ThemeColors.TextMuted,
            VerticalOptions = LayoutOptions.Center
        };
        var val = new Label
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
            Children = { lbl }
        };
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);
        return grid;
    }

    private async Task RestartAsync(string gameId)
    {
        var def = GameCatalog.Get(gameId);
        if (def is null) { await Navigation.PopToRootAsync(); return; }
        var nav = Navigation;
        var newGame = def.CreatePage();
        nav.InsertPageBefore(newGame, this);
        await nav.PopAsync(animated: false);
    }

    private async Task ShareResult(GameResult result)
    {
        var text = $"Сыграл(а) в «{result.GameTitle}» в CogniBoost!\n" +
                   $"Счёт: {result.Score} из {result.MaxScore} ({result.AccuracyPercent}%)" +
                   $"{(result.EarnedPoints > 0 ? $"\nЗаработано: +{result.EarnedPoints} ⭐" : "")}" +
                   $"\n\nСкачать: {SettingsService.AppDownloadUrl}";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = $"CogniBoost — {result.GameTitle}",
            Text = text
        });
    }
}
