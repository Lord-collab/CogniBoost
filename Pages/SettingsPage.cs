using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.ApplicationModel;
using Permissions = Microsoft.Maui.ApplicationModel.Permissions;
using Microsoft.Maui.Devices;

namespace CogniBoost.Pages;

/// <summary>Настройки приложения: тема, звук, крупный текст, уведомления, сброс, поделиться.</summary>
public partial class SettingsPage : ContentPage
{
    private static readonly (string Label, string Value)[] Themes =
    {
        ("Системная", "system"),
        ("Светлая",   "light"),
        ("Тёмная",    "dark"),
    };

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSettings();
        _ = LoadProfileAsync();
    }

    // ── Загрузка ──────────────────────────────────────────────────

    private void LoadSettings()
    {
        UpdateThemeChips(SettingsService.Theme);

        LargeTextSwitch.IsToggled = SettingsService.LargeText;
        SoundSwitch.IsToggled = SettingsService.SoundEnabled;

        NotifSwitch.IsToggled = NotificationService.IsEnabled;
        NotificationTimePicker.Time = NotificationService.ReminderTime;
        TimePickerSection.IsVisible = NotificationService.IsEnabled;

        VersionLabel.Text = $"CogniBoost v{SettingsService.AppVersion}";
    }

    private async Task LoadProfileAsync()
    {
        var profile = await AccountStore.GetProfileAsync();
        if (profile is null) return;

        NameEntry.Text = profile.Username;
        AgeEntry.Text = profile.Age > 0 ? profile.Age.ToString() : string.Empty;
    }

    // ── Тема ──────────────────────────────────────────────────────

    private void UpdateThemeChips(string theme)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        foreach (var (chip, label, value) in new[]
        {
            (SystemChip, SystemChipLabel, "system"),
            (LightChip, LightChipLabel, "light"),
            (DarkChip, DarkChipLabel, "dark"),
        })
        {
            var selected = value == theme;
            if (selected)
            {
                chip.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
                chip.Stroke = Colors.Transparent;
                chip.StrokeThickness = 0;
                label.TextColor = Colors.White;
            }
            else
            {
                chip.BackgroundColor = (Color)Application.Current!.Resources[
                    isDark ? "CardSecondaryDark" : "CardSecondaryLight"];
                chip.Stroke = (Color)Application.Current!.Resources[
                    isDark ? "CardBorderDark" : "CardBorderLight"];
                chip.StrokeThickness = 1.5;
                label.TextColor = (Color)Application.Current!.Resources[
                    isDark ? "TextPrimaryDark" : "TextPrimaryLight"];
            }
        }
    }

    private async void OnThemeChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string theme) return;
        if (theme == SettingsService.Theme) return;

        await SettingsService.SetThemeAsync(theme);
        SettingsService.ApplyTheme();
        UpdateThemeChips(theme);
    }

    // ── Аккаунт ───────────────────────────────────────────────────

    private async void OnSaveProfileClicked(object? sender, EventArgs e)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            var (success, err) = await AccountStore.TryUpdateDisplayNameAsync(NameEntry.Text);
            if (!success) errors.Add(err);
        }

        if (!string.IsNullOrWhiteSpace(AgeEntry.Text))
        {
            var (success, err) = await AccountStore.TryUpdateAgeAsync(AgeEntry.Text);
            if (!success) errors.Add(err);
        }

        if (errors.Count > 0)
        {
            await DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
            return;
        }

        await DisplayAlertAsync("Готово", "Данные профиля обновлены.", "OK");
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ChangePasswordPage());
    }

    private async void OnChangeDirectionsClicked(object? sender, EventArgs e)
    {
        await AccountStore.ResetOnboardingAsync();
        App.ResetRootPage();
    }

    // ── Доступность ───────────────────────────────────────────────

    private async void OnLargeTextToggled(object? sender, ToggledEventArgs e)
    {
        await SettingsService.SetLargeTextAsync(e.Value);
        SettingsService.ApplyAll();
    }

    // ── Звук ──────────────────────────────────────────────────────

    private async void OnSoundToggled(object? sender, ToggledEventArgs e)
    {
        await SettingsService.SetSoundEnabledAsync(e.Value);
    }

    // ── Уведомления ───────────────────────────────────────────────

    private async void OnNotifToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value && DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Ошибка",
                        "Для уведомлений нужно разрешение. Включите его в настройках системы.",
                        "OK");
                    NotifSwitch.IsToggled = false;
                    return;
                }
            }
        }

        NotificationService.IsEnabled = e.Value;
        TimePickerSection.IsVisible = e.Value;
        if (e.Value) await NotificationService.ScheduleDailyReminderAsync();
        else NotificationService.CancelAll();
    }

    private async void OnNotifTimeChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimePicker.Time))
        {
            NotificationService.ReminderTime = NotificationTimePicker.Time ?? TimeSpan.FromHours(20);
            if (NotificationService.IsEnabled)
                await NotificationService.ScheduleDailyReminderAsync();
        }
    }

    // ── О приложении ──────────────────────────────────────────────

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        var overall = await ProgressStore.GetOverallScoreAsync();
        var version = SettingsService.AppVersion;
        var text = overall > 0
            ? $"CogniBoost v{version} — Тренирую мозг! Мой индекс мозга: {overall} 🧠\nСкачать: {SettingsService.AppDownloadUrl}"
            : $"CogniBoost v{version} — Тренируй память, внимание и логику каждый день!\nСкачать: {SettingsService.AppDownloadUrl}";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "CogniBoost",
            Text = text,
            Uri = SettingsService.AppDownloadUrl
        });
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        var overall = await ProgressStore.GetOverallScoreAsync();
        var games = await ProgressStore.GetGamesPlayedCountAsync();
        var bestIq = await ProgressStore.GetBestIqAsync();
        var streak = await StreakService.GetCurrentStreakAsync();
        var longest = await StreakService.GetLongestStreakAsync();
        var balance = await PointsService.GetBalanceAsync();
        var lifetime = await PointsService.GetLifetimeEarnedAsync();
        var ach = await AchievementsService.UnlockedCountAsync();
        var total = AchievementsService.TotalCount();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📊 CogniBoost — Моя статистика");
        sb.AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy}");
        sb.AppendLine();
        sb.AppendLine($"🧠 Индекс мозга: {overall}");
        sb.AppendLine($"🎮 Сыграно игр: {games}");
        sb.AppendLine($"📝 Лучший IQ: {(bestIq > 0 ? bestIq.ToString() : "—")}");
        sb.AppendLine($"🔥 Текущая серия: {streak} дн.");
        sb.AppendLine($"🏅 Лучшая серия: {longest} дн.");
        sb.AppendLine($"⭐ Баланс бонусов: {balance}");
        sb.AppendLine($"💫 Всего заработано: {lifetime}");
        sb.AppendLine($"🏆 Достижения: {ach}/{total}");
        sb.AppendLine();

        foreach (var meta in BrainSkillInfo.All)
        {
            var score = await ProgressStore.GetSkillScoreAsync(meta.Skill);
            sb.AppendLine($"{meta.Emoji} {meta.Title}: {score}/1000");
        }

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Моя статистика CogniBoost",
            Text = sb.ToString()
        });
    }

    private async void OnResetProgressClicked(object? sender, EventArgs e)
    {
        var first = await DisplayAlertAsync(
            "Сброс прогресса",
            "Это удалит всю историю игр, тестов, бонусы, достижения и streak. Аккаунт останется.\n\nПродолжить?",
            "Да, сбросить", "Отмена");
        if (!first) return;

        var second = await DisplayAlertAsync(
            "Подтверждение",
            "Вы уверены? Это действие нельзя отменить.",
            "Удалить всё", "Отмена");
        if (!second) return;

        await AccountStore.ResetProgressAsync();
        await DisplayAlertAsync("Готово", "Прогресс сброшен.", "OK");
    }
}
