using CogniBoost.Models;
using CogniBoost.Pages.Controls;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public partial class TrainingPage : ContentPage
{
    public TrainingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RefreshAsync();
        SettingsService.ApplyTextScale(this);
    }

    private async Task RefreshAsync()
    {
        var profile = await AccountStore.GetProfileAsync();

        // Аватар и приветствие
        AvatarLabel.Text = profile?.AvatarEmoji ?? "🧠";
        GreetingLabel.Text = !string.IsNullOrWhiteSpace(profile?.Username)
            ? $"Привет, {profile.Username}! 👋"
            : "Привет! 👋";

        SubGreetingLabel.Text = $"Готов прокачать мозг?";

        // Статистика
        BrainScoreLabel.Text = (await ProgressStore.GetOverallScoreAsync()).ToString();
        GamesPlayedLabel.Text = (await ProgressStore.GetGamesPlayedCountAsync()).ToString();
        PointsLabel.Text = $"{await PointsService.GetBalanceAsync()} ⭐";

        // Streak
        var streak = await StreakService.GetCurrentStreakAsync();
        StreakLabel.Text = streak == 0 ? "Начни сегодня" : $"{streak} {DayWord(streak)}";
        var streakBonus = streak >= 3 ? 10 * (streak / 3) : 0;
        StreakBonusLabel.Text = streakBonus > 0 ? $"+{streakBonus} ⭐/день" : "";

        // Гостевой режим
        GuestBanner.IsVisible = AccountStore.IsGuest;

        // Ежедневный челлендж
        BuildChallenge();

        // Ежедневные игры
        await BuildDailyAsync(profile);
    }

    private static string DayWord(int n) => n switch
    {
        1 => "день",
        2 or 3 or 4 => "дня",
        _ => "дней"
    };

    private void BuildChallenge()
    {
        var challenge = DailyChallengeService.GetTodayChallenge();
        if (challenge is not null)
        {
            ChallengeBorder.IsVisible = true;
            ChallengeTitleLabel.Text = challenge.Emoji + "  " + challenge.Title;

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => await Navigation.PushAsync(challenge.CreatePage());
            ChallengeBorder.GestureRecognizers.Clear();
            ChallengeBorder.GestureRecognizers.Add(tap);
        }
        else
        {
            ChallengeBorder.IsVisible = false;
        }
    }

    private async Task BuildDailyAsync(UserProfile? profile)
    {
        DailyLayout.Children.Clear();

        var selectedSkills = profile?.SelectedSkills ?? new List<BrainSkill>();
        IEnumerable<GameDefinition> pool = GameCatalog.All.Where(UnlockService.IsUnlocked);

        if (selectedSkills.Count > 0)
        {
            var preferred = pool.Where(g => selectedSkills.Contains(g.Skill)).ToList();
            if (preferred.Count > 0) pool = preferred;
        }

        var rng = new Random();
        var daily = pool.OrderBy(_ => rng.Next()).Take(3).ToList();

        if (daily.Count == 0)
        {
            DailyLayout.Children.Add(new EmptyState(
                "🎯", "Нет доступных игр",
                "Сыграй в каталоге, чтобы открыть новые.",
                ""));
            return;
        }

        foreach (var game in daily)
            DailyLayout.Children.Add(await BuildDailyCardAsync(game));
    }

    private async Task<View> BuildDailyCardAsync(GameDefinition game)
    {
        var accent = BrainSkillInfo.Accent(game.Skill);
        var best = await ProgressStore.GetBestScoreAsync(game.Id);

        var emoji = new Label
        {
            Text = game.Emoji,
            FontSize = 28,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 44,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var info = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = game.Title,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColors.TextPrimary
                },
                new Label
                {
                    Text = $"{BrainSkillInfo.Title(game.Skill)}{(best > 0 ? $"  ·  {best}" : "")}",
                    FontSize = 11,
                    TextColor = accent
                }
            }
        };

        var arrow = new Label
        {
            Text = "▶",
            FontSize = 14,
            TextColor = accent,
            VerticalOptions = LayoutOptions.Center
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            Children = { emoji, info, arrow }
        };
        Grid.SetColumn(info, 1);
        Grid.SetColumn(arrow, 2);

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = new Thickness(14),
            Content = grid,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = new Point(0, 1),
                Radius = 6,
                Opacity = 0.05f
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Navigation.PushAsync(game.CreatePage());
        card.GestureRecognizers.Add(tap);

        return card;
    }

    private async void OnAllGamesClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//games");

    private async void OnGuestLoginClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnQuickGameClicked(object? sender, EventArgs e)
    {
        var profile = await AccountStore.GetProfileAsync();
        var selectedSkills = profile?.SelectedSkills ?? new List<BrainSkill>();

        IEnumerable<GameDefinition> pool = GameCatalog.All.Where(UnlockService.IsUnlocked);
        if (selectedSkills.Count > 0)
        {
            var preferred = pool.Where(g => selectedSkills.Contains(g.Skill)).ToList();
            if (preferred.Count > 0) pool = preferred;
        }

        var available = pool.ToList();
        if (available.Count == 0)
        {
            await DisplayAlertAsync("Нет игр", "Сыграй хотя бы одну игру из каталога.", "OK");
            return;
        }

        var game = available[new Random().Next(available.Count)];
        await Navigation.PushAsync(game.CreatePage());
    }
}
