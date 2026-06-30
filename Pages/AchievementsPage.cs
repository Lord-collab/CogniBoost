using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>Список всех достижений (24 шт.) с индикатором разблокировки и датой.</summary>
public sealed class AchievementsPage : ContentPage
{
    public AchievementsPage()
    {
        Title = "Достижения";
        BackgroundColor = ThemeColors.PageBg;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Content = null;
        _ = BuildUiAsync();
    }

    private async Task BuildUiAsync()
    {
        var all      = await AchievementsService.GetAllAsync();
        var unlocked = all.Count(a => a.IsUnlocked);

        var header = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.Accent,
            Padding = new Thickness(18, 14),
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = "Твои достижения", FontSize = 14,
                        TextColor = ThemeColors.AccentLight },
                    new Label { Text = $"{unlocked} / {all.Count}",
                        FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
                }
            }
        };

        var list = new VerticalStackLayout { Spacing = 10 };
        foreach (var ach in all.OrderByDescending(a => a.IsUnlocked).ThenBy(a => a.Title))
            list.Children.Add(BuildCard(ach));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16),
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Достижения", FontSize = 28,
                        FontAttributes = FontAttributes.Bold, TextColor = ThemeColors.TextPrimary },
                    header,
                    list
                }
            }
        };
    }

    private static View BuildCard(Services.Achievement ach)
    {
        var locked = !ach.IsUnlocked;

        var emoji = new Label
        {
            Text = locked ? "🔒" : ach.Emoji,
            FontSize = 32, VerticalOptions = LayoutOptions.Center,
            Opacity = locked ? 0.4 : 1.0
        };

        var title = new Label
        {
            Text = ach.Title, FontSize = 15, FontAttributes = FontAttributes.Bold,
            TextColor = locked ? ThemeColors.TextMuted : ThemeColors.TextPrimary
        };
        var desc = new Label
        {
            Text = ach.Description, FontSize = 12,
            TextColor = ThemeColors.TextSecondary
        };
        var dateLabel = ach.UnlockedAt.HasValue
            ? new Label { Text = ach.UnlockedAt.Value.ToLocalTime().ToString("dd.MM.yyyy"),
                FontSize = 11, TextColor = ThemeColors.Accent }
            : null;

        var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        info.Children.Add(title);
        info.Children.Add(desc);
        if (dateLabel is not null) info.Children.Add(dateLabel);
        Grid.SetColumn(info, 1);

        var grid = new Grid
        {
            ColumnSpacing = 14,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Children = { emoji, info }
        };

        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            BackgroundColor = locked ? ThemeColors.CardBg2 : ThemeColors.CardBg,
            Padding = new Thickness(14),
            Opacity = locked ? 0.65 : 1.0,
            Content = grid
        };
    }
}
