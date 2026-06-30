using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>Таблица лидеров: онлайн-рейтинг из Supabase или локальный фолбэк.</summary>
public sealed class LeaderboardPage : ContentPage
{
    private readonly VerticalStackLayout _list = new() { Spacing = 8 };
    private readonly Label _statusLabel = new();
    private readonly ActivityIndicator _spinner = new() { IsRunning = false, Color = ThemeColors.Accent };

    public LeaderboardPage()
    {
        Title = "Рейтинг";
        BackgroundColor = ThemeColors.PageBg;
        BuildUi();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private void BuildUi()
    {
        _statusLabel.FontSize = 12;
        _statusLabel.TextColor = ThemeColors.TextMuted;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16),
                Spacing = 14,
                Children =
                {
                    new Label { Text = "🏆 Таблица лидеров", FontSize = 26,
                        FontAttributes = FontAttributes.Bold, TextColor = ThemeColors.TextPrimary },
                    _statusLabel,
                    _spinner,
                    _list
                }
            }
        };
    }

    private async Task LoadAsync()
    {
        _spinner.IsRunning = true;
        _list.Children.Clear();

        var result = await LeaderboardService.GetLeaderboardAsync();
        _statusLabel.Text = result.Message;
        _spinner.IsRunning = false;

        foreach (var entry in result.Entries)
            _list.Children.Add(BuildRow(entry, result.IsLive));
    }

    private View BuildRow(LeaderboardEntry entry, bool isLive)
    {
        var highlight = entry.IsCurrentPlayer;
        var rankText = entry.Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{entry.Rank}" };

        var rank = new Label
        {
            Text = rankText,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 40,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
            TextColor = entry.Rank <= 3 ? ThemeColors.Warning : ThemeColors.TextSecondary
        };

        var avatar = new Label
        {
            Text = entry.AvatarEmoji,
            FontSize = 26,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(avatar, 1);

        var name = new Label
        {
            Text = entry.IsCurrentPlayer ? $"{entry.Name} (вы)" : entry.Name,
            FontSize = 15,
            FontAttributes = highlight ? FontAttributes.Bold : FontAttributes.None,
            TextColor = highlight ? ThemeColors.Accent : ThemeColors.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(name, 2);

        var score = new Label
        {
            Text = entry.Score.ToString(),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColors.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(score, 3);

        var grid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { rank, avatar, name, score }
        };

        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = highlight ? ThemeColors.Accent : Colors.Transparent,
            StrokeThickness = highlight ? 2 : 0,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 12,
            Content = grid
        };

        if (isLive && !entry.IsCurrentPlayer)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => await ShowPlayerProfile(entry);
            border.GestureRecognizers.Add(tap);
        }

        return border;
    }

    private async Task ShowPlayerProfile(LeaderboardEntry entry)
    {
        var message = $"{entry.AvatarEmoji}  {entry.Name}\n\nИндекс мозга: {entry.Score}";
        await DisplayAlertAsync($"Игрок #{entry.Rank}", message, "Закрыть");
    }
}