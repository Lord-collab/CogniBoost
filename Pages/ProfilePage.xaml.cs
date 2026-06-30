using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>Вкладка «Профиль»: аватар, имя, возраст, стрик, баланс, заработано, достижения, навыки.</summary>
public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var profile = await AccountStore.GetProfileAsync();
        if (profile is null) return;

        AvatarLabel.Text   = profile.AvatarEmoji;
        UsernameLabel.Text = profile.Username;
        AgeLabel.Text      = profile.Age > 0 ? $"{profile.Age} лет" : string.Empty;

        var streak = await StreakService.GetCurrentStreakAsync();
        StreakLabel.Text  = streak > 0 ? $"🔥 {streak} дн." : string.Empty;
        BalanceLabel.Text = (await PointsService.GetBalanceAsync()).ToString();
        LifetimeLabel.Text = (await PointsService.GetLifetimeEarnedAsync()).ToString();

        var unlocked = await AchievementsService.UnlockedCountAsync();
        var total    = AchievementsService.TotalCount();
        AchievementsCountLabel.Text = $"{unlocked} / {total}";

        var fraction = total > 0 ? (double)unlocked / total : 0;
        AchievementFill.WidthRequest = AchievementBar.Width * fraction;
        AchievementBar.SizeChanged += (_, _) =>
        {
            if (AchievementBar.Width > 0)
                AchievementFill.WidthRequest = AchievementBar.Width * fraction;
        };

        SkillsLayout.Children.Clear();
        if (profile.SelectedSkills.Count > 0)
        {
            foreach (var skill in profile.SelectedSkills)
            {
                var meta = BrainSkillInfo.Get(skill);
                var chip = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Stroke = Colors.Transparent,
                    BackgroundColor = BrainSkillInfo.Accent(skill).WithAlpha(0.15f),
                    Padding = new Thickness(10, 5),
                    Margin = new Thickness(0, 0, 6, 6),
                    Content = new Label
                    {
                        Text = $"{meta.Emoji} {meta.Title}",
                        FontSize = 13,
                        TextColor = BrainSkillInfo.Accent(skill)
                    }
                };
                SkillsLayout.Children.Add(chip);
            }
        }
        else
        {
            SkillsLayout.Children.Add(new Label
            {
                Text = "Направления не выбраны",
                FontSize = 13,
                TextColor = ThemeColors.TextMuted
            });
        }
    }

    private async void OnChangeAvatarClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new AvatarPickerPage());

    private async void OnAchievementsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new AchievementsPage());

    private async void OnStoreClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("store");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("settings");

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Выход", "Выйти из аккаунта?", "Выйти", "Отмена");
        if (!confirm) return;
        AccountStore.SignOut();
        App.ResetRootPage();
    }
}
