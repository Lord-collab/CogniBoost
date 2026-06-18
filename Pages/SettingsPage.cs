using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.ApplicationModel;
using Permissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace CogniBoost.Pages;

public sealed class SettingsPage : ContentPage
{
    private static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;
    private static Color Bg => Color.FromArgb(IsDark ? "#0A0B18" : "#F0F1FA");
    private static Color Card => Color.FromArgb(IsDark ? "#13142A" : "#FFFFFF");
    private static Color Txt => Color.FromArgb(IsDark ? "#E8E8FF" : "#0D0D2B");
    private static Color Muted => Color.FromArgb(IsDark ? "#6B6C99" : "#7B7BA8");

    // Новые динамические цвета для адаптации обводок и меток
    private static Color RowText => Color.FromArgb(IsDark ? "#E8E8FF" : "#0D0D2B");
    private static Color BorderStroke => Color.FromArgb(IsDark ? "#2A2B5C" : "#E4E5F5");
    private static Color EntryText => Color.FromArgb(IsDark ? "#E8E8FF" : "#0D0D2B");

    private readonly Picker _themePicker = new();
    private readonly Picker _langPicker = new();
    private readonly Switch _largeTextSwitch = new();
    private readonly Switch _notifSwitch = new();
    private readonly TimePicker _timePicker = new();
    private readonly Entry _nameEntry = new() { Keyboard = Keyboard.Text };
    private readonly Entry _ageEntry = new() { Keyboard = Keyboard.Numeric };

    private static readonly (string Label, string Value)[] Themes =
    {
        (L10n.T("Settings_ThemeSystem"), "system"),
        (L10n.T("Settings_ThemeLight"),  "light"),
        (L10n.T("Settings_ThemeDark"),   "dark"),
    };

    private static readonly (string Label, string Value)[] Languages =
    {
        ("Русский", "ru"),
        ("English", "en"),
    };

    public SettingsPage()
    {
        Title = L10n.T("Settings_Title");
        BackgroundColor = Bg;
        BuildUi();
    }

    private void BuildUi()
    {
        // ── Аккаунт ───────────────────────────────────────────────────
        AccountStore.TryGetCurrentProfile(out var profile);

        _nameEntry.Text = profile?.Username ?? string.Empty;
        _nameEntry.Placeholder = L10n.T("Settings_DisplayName");
        _nameEntry.BackgroundColor = Card;
        _nameEntry.TextColor = EntryText;

        _ageEntry.Text = profile?.Age > 0 ? profile.Age.ToString() : string.Empty;
        _ageEntry.Placeholder = L10n.T("Settings_Age");
        _ageEntry.BackgroundColor = Card;
        _ageEntry.TextColor = EntryText;

        var saveProfileBtn = MakeButton(L10n.T("Common_Save"), "#6C63FF");
        saveProfileBtn.Clicked += OnSaveProfile;

        var changePasswordBtn = MakeButton(L10n.T("Settings_ChangePassword"), "#9B59F5");
        changePasswordBtn.Clicked += async (_, _) => await Navigation.PushAsync(new ChangePasswordPage());

        var changeDirectionsBtn = MakeButton(L10n.T("Settings_ChangeDirections"), "#1DE9C8", textColor: "#0D0D2B");
        changeDirectionsBtn.Clicked += OnChangeDirections;

        var accountSection = BuildSection(L10n.T("Settings_Account"), new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                BuildLabeledEntry(L10n.T("Settings_DisplayName"), _nameEntry),
                BuildLabeledEntry(L10n.T("Settings_Age"), _ageEntry),
                saveProfileBtn,
                changePasswordBtn,
                changeDirectionsBtn,
            }
        });

        // ── Оформление ────────────────────────────────────────────────
        foreach (var (label, _) in Themes) _themePicker.Items.Add(label);
        _themePicker.SelectedIndex = Math.Max(0, Array.FindIndex(Themes, t => t.Value == SettingsService.Theme));
        _themePicker.BackgroundColor = Card;
        _themePicker.TextColor = EntryText;
        _themePicker.SelectedIndexChanged += (_, _) =>
        {
            var idx = _themePicker.SelectedIndex;
            if (idx >= 0 && idx < Themes.Length)
            {
                SettingsService.Theme = Themes[idx].Value;
                SettingsService.ApplyTheme();
            }
        };

        foreach (var (label, _) in Languages) _langPicker.Items.Add(label);
        _langPicker.SelectedIndex = Math.Max(0, Array.FindIndex(Languages, l => l.Value == SettingsService.Language));
        _langPicker.BackgroundColor = Card;
        _langPicker.TextColor = EntryText;
        _langPicker.SelectedIndexChanged += (_, _) =>
        {
            var idx = _langPicker.SelectedIndex;
            if (idx >= 0 && idx < Languages.Length)
            {
                SettingsService.Language = Languages[idx].Value;
                L10n.Load();
                App.ResetRootPage();
            }
        };

        var appearanceSection = BuildSection(L10n.T("Settings_Appearance"), new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                BuildLabeledPicker(L10n.T("Settings_Theme"), _themePicker),
                BuildLabeledPicker(L10n.T("Settings_Language"), _langPicker),
            }
        });

        // ── Доступность ───────────────────────────────────────────────
        _largeTextSwitch.IsToggled = SettingsService.LargeText;
        _largeTextSwitch.OnColor = Color.FromArgb("#6C63FF");
        _largeTextSwitch.Toggled += (_, e) =>
        {
            SettingsService.LargeText = e.Value;
            SettingsService.ApplyAll();
        };

        var accessibilitySection = BuildSection(L10n.T("Settings_Accessibility"), new VerticalStackLayout
        {
            Spacing = 10,
            Children = { BuildToggleRow(L10n.T("Settings_LargeText"), _largeTextSwitch) }
        });

        // ── Уведомления ───────────────────────────────────────────────
        _notifSwitch.IsToggled = NotificationService.IsEnabled;
        _notifSwitch.OnColor = Color.FromArgb("#6C63FF");
        _notifSwitch.Toggled += async (_, e) =>
        {
            if (e.Value && DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlertAsync(L10n.T("Common_Error"),
                            "Для уведомлений нужно разрешение. Включите его в настройках системы.",
                            L10n.T("Common_OK"));
                        _notifSwitch.IsToggled = false;
                        return;
                    }
                }
            }

            NotificationService.IsEnabled = e.Value;
            _timePicker.IsVisible = e.Value;
            if (e.Value) await NotificationService.ScheduleDailyReminderAsync();
            else NotificationService.CancelAll();
        };

        _timePicker.Time = NotificationService.ReminderTime;
        _timePicker.IsVisible = NotificationService.IsEnabled;
        _timePicker.BackgroundColor = Card;
        _timePicker.TextColor = EntryText;
        _timePicker.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(TimePicker.Time))
            {
                NotificationService.ReminderTime = _timePicker.Time ?? TimeSpan.FromHours(20);
                if (NotificationService.IsEnabled)
                    await NotificationService.ScheduleDailyReminderAsync();
            }
        };

        var notifHint = new Label
        {
            Text = "Уведомление придёт в выбранное время каждый день.",
            FontSize = 12,
            TextColor = Muted
        };

        var notifSection = BuildSection(L10n.T("Settings_Notifications"), new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                BuildToggleRow(L10n.T("Settings_NotifEnabled"), _notifSwitch),
                BuildLabeledView(L10n.T("Settings_NotifTime"), _timePicker),
                notifHint,
            }
        });

        // ── О приложении ──────────────────────────────────────────────
        var shareBtn = MakeButton(L10n.T("Settings_Share"), "#6C63FF");
        shareBtn.Clicked += async (_, _) => await ShareApp();

        var exportBtn = MakeButton(L10n.T("Settings_ExportStats"), "#1DE9C8", textColor: "#0D0D2B");
        exportBtn.Clicked += async (_, _) => await ExportStats();

        var resetBtn = MakeButton(L10n.T("Settings_ResetProgress"), "Transparent",
            textColor: "#FF5370", borderColor: "#FF5370");
        resetBtn.Clicked += async (_, _) => await ResetProgress();

        var versionLabel = new Label
        {
            Text = $"CogniBoost v{SettingsService.AppVersion}",
            FontSize = 13,
            TextColor = Muted,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var aboutSection = BuildSection(L10n.T("Settings_About"), new VerticalStackLayout
        {
            Spacing = 10,
            Children = { shareBtn, exportBtn, resetBtn, versionLabel }
        });

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = L10n.T("Settings_Title"), FontSize = 28,
                        FontAttributes = FontAttributes.Bold, TextColor = Txt },
                    accountSection,
                    appearanceSection,
                    accessibilitySection,
                    notifSection,
                    aboutSection,
                }
            }
        };
    }

    // ── Обработчики ──────────────────────────────────────────────────
    private async void OnSaveProfile(object? sender, EventArgs e)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(_nameEntry.Text))
        {
            if (!AccountStore.TryUpdateDisplayName(_nameEntry.Text, out var err)) errors.Add(err);
        }

        if (!string.IsNullOrWhiteSpace(_ageEntry.Text))
        {
            if (!AccountStore.TryUpdateAge(_ageEntry.Text, out var err)) errors.Add(err);
        }

        if (errors.Count > 0)
        {
            await DisplayAlertAsync(L10n.T("Common_Error"), string.Join("\n", errors), L10n.T("Common_OK"));
            return;
        }

        await DisplayAlertAsync(L10n.T("Common_Success"), "Данные профиля обновлены.", L10n.T("Common_OK"));
    }

    private void OnChangeDirections(object? sender, EventArgs e)
    {
        AccountStore.ResetOnboarding();
        App.ResetRootPage();
    }

    private async Task ShareApp()
    {
        var best = ProgressStore.GetOverallScore();
        var text = best > 0
            ? $"Тренирую мозг в CogniBoost! Мой индекс мозга: {best} 🧠\nСкачать: {SettingsService.AppDownloadUrl}"
            : $"Тренируй память, внимание и логику каждый день!\nCogniBoost: {SettingsService.AppDownloadUrl}";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "CogniBoost",
            Text = text,
            Uri = SettingsService.AppDownloadUrl
        });
    }

    private async Task ExportStats()
    {
        var overall = ProgressStore.GetOverallScore();
        var games = ProgressStore.GetGamesPlayedCount();
        var bestIq = ProgressStore.GetBestIq();
        var streak = StreakService.GetCurrentStreak();
        var longest = StreakService.GetLongestStreak();
        var balance = PointsService.GetBalance();
        var lifetime = PointsService.GetLifetimeEarned();
        var ach = AchievementsService.UnlockedCount();
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
            var score = ProgressStore.GetSkillScore(meta.Skill);
            sb.AppendLine($"{meta.Emoji} {meta.Title}: {score}/1000");
        }

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Моя статистика CogniBoost",
            Text = sb.ToString()
        });
    }

    private async Task ResetProgress()
    {
        var first = await DisplayAlertAsync(
            "Сброс прогресса",
            "Это удалит всю историю игр, тестов, бонусы, достижения и streak. Аккаунт останется.\n\nПродолжить?",
            "Да, сбросить", L10n.T("Common_Cancel"));
        if (!first) return;

        var second = await DisplayAlertAsync(
            "Подтверждение",
            "Вы уверены? Это действие нельзя отменить.",
            "Удалить всё", L10n.T("Common_Cancel"));
        if (!second) return;

        AccountStore.ResetProgress();
        await DisplayAlertAsync(L10n.T("Common_Success"), "Прогресс сброшен.", L10n.T("Common_OK"));
    }

    // ── Вспомогательные ──────────────────────────────────────────────
    private Border BuildSection(string title, View content)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Stroke = Colors.Transparent,
            BackgroundColor = Card,
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold,
                        TextColor = Muted },
                    content
                }
            }
        };
    }

    private static Button MakeButton(string text, string bg, string textColor = "White", string borderColor = "Transparent")
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(bg),
            TextColor = Color.FromArgb(textColor),
            BorderColor = Color.FromArgb(borderColor),
            BorderWidth = borderColor == "Transparent" ? 0 : 1.5,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50,
            CornerRadius = 14
        };
    }

    // Использован динамический цвет RowText вместо жестко заданного черного
    private View BuildToggleRow(string label, Switch sw)
    {
        var lbl = new Label
        {
            Text = label,
            FontSize = 15,
            TextColor = RowText,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(sw, 1);
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { lbl, sw }
        };
    }

    // Использован динамический цвет BorderStroke и Muted для меток
    private View BuildLabeledEntry(string label, Entry entry)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = label, FontSize = 12, TextColor = Muted },
                new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = BorderStroke, StrokeThickness = 1,
                    Padding = new Thickness(12, 4),
                    Content = entry
                }
            }
        };
    }

    private View BuildLabeledPicker(string label, Picker picker)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = label, FontSize = 12, TextColor = Muted },
                new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = BorderStroke, StrokeThickness = 1,
                    Padding = new Thickness(12, 4),
                    Content = picker
                }
            }
        };
    }

    private View BuildLabeledView(string label, View view)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = label, FontSize = 12, TextColor = Muted },
                new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = BorderStroke, StrokeThickness = 1,
                    Padding = new Thickness(12, 4),
                    Content = view
                }
            }
        };
    }
}