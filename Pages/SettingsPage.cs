using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.ApplicationModel;
using Permissions = Microsoft.Maui.ApplicationModel.Permissions;
using Microsoft.Maui.Devices;

namespace CogniBoost.Pages;

public sealed class SettingsPage : ContentPage
{
    private readonly Picker _themePicker = new();
    private readonly Switch _largeTextSwitch = new();
    private readonly Switch _notifSwitch = new();
    private readonly TimePicker _timePicker = new();
    private readonly Entry _nameEntry = new() { Keyboard = Keyboard.Text };
    private readonly Entry _ageEntry = new() { Keyboard = Keyboard.Numeric };

    private static readonly (string Label, string Value)[] Themes =
    {
        ("Системная", "system"),
        ("Светлая",   "light"),
        ("Тёмная",    "dark"),
    };

    public SettingsPage()
    {
        Title = "Настройки";
        BackgroundColor = ThemeColors.PageBg;
        BuildUi();
    }

    private void BuildUi()
    {
        AccountStore.TryGetCurrentProfile(out var profile);

        _nameEntry.Text = profile?.Username ?? string.Empty;
        _nameEntry.Placeholder = "Отображаемое имя";
        _nameEntry.BackgroundColor = ThemeColors.CardBg;
        _nameEntry.TextColor = ThemeColors.TextPrimary;

        _ageEntry.Text = profile?.Age > 0 ? profile.Age.ToString() : string.Empty;
        _ageEntry.Placeholder = "Возраст";
        _ageEntry.BackgroundColor = ThemeColors.CardBg;
        _ageEntry.TextColor = ThemeColors.TextPrimary;

        var saveProfileBtn = MakeButton("Сохранить", ThemeColors.Accent);
        saveProfileBtn.Clicked += OnSaveProfile;

        var changePasswordBtn = MakeButton("Сменить пароль", ThemeColors.Tertiary);
        changePasswordBtn.Clicked += async (_, _) => await Navigation.PushAsync(new ChangePasswordPage());

        var changeDirectionsBtn = MakeButton("Изменить направления", ThemeColors.Secondary, textColorC: Colors.White);
        changeDirectionsBtn.Clicked += OnChangeDirections;

        var accountSection = BuildSection("Аккаунт", new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                BuildLabeledEntry("Отображаемое имя", _nameEntry),
                BuildLabeledEntry("Возраст", _ageEntry),
                saveProfileBtn,
                changePasswordBtn,
                changeDirectionsBtn,
            }
        });

        // ── Оформление ────────────────────────────────────────────────
        foreach (var (label, _) in Themes) _themePicker.Items.Add(label);
        _themePicker.SelectedIndex = Math.Max(0, Array.FindIndex(Themes, t => t.Value == SettingsService.Theme));
        _themePicker.BackgroundColor = ThemeColors.CardBg;
        _themePicker.TextColor = ThemeColors.TextPrimary;
        _themePicker.SelectedIndexChanged += (_, _) =>
        {
            var idx = _themePicker.SelectedIndex;
            if (idx >= 0 && idx < Themes.Length)
            {
                SettingsService.Theme = Themes[idx].Value;
                SettingsService.ApplyTheme();
            }
        };

        var appearanceSection = BuildSection("Оформление", new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                BuildLabeledPicker("Тема", _themePicker),
            }
        });

        // ── Доступность ───────────────────────────────────────────────
        _largeTextSwitch.IsToggled = SettingsService.LargeText;
        _largeTextSwitch.OnColor = ThemeColors.Accent;
        _largeTextSwitch.Toggled += (_, e) =>
        {
            SettingsService.LargeText = e.Value;
            SettingsService.ApplyAll();
        };

        var accessibilitySection = BuildSection("Доступность", new VerticalStackLayout
        {
            Spacing = 10,
            Children = { BuildToggleRow("Крупный текст", _largeTextSwitch) }
        });

        // ── Звук ──────────────────────────────────────────────────────
        var soundSwitch = new Switch
        {
            IsToggled = SettingsService.SoundEnabled,
            OnColor = ThemeColors.Accent
        };
        soundSwitch.Toggled += (_, e) => SettingsService.SoundEnabled = e.Value;

        var soundSection = BuildSection("Звук", new VerticalStackLayout
        {
            Spacing = 10,
            Children = { BuildToggleRow("Звуковые эффекты", soundSwitch) }
        });

        // ── Уведомления ───────────────────────────────────────────────
        _notifSwitch.IsToggled = NotificationService.IsEnabled;
        _notifSwitch.OnColor = ThemeColors.Accent;
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
                        await DisplayAlertAsync("Ошибка",
                            "Для уведомлений нужно разрешение. Включите его в настройках системы.",
                            "OK");
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
        _timePicker.BackgroundColor = ThemeColors.CardBg;
        _timePicker.TextColor = ThemeColors.TextPrimary;
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
            TextColor = ThemeColors.TextMuted
        };

        var notifSection = BuildSection("Уведомления", new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                BuildToggleRow("Напоминание о тренировке", _notifSwitch),
                BuildLabeledView("Время напоминания", _timePicker),
                notifHint,
            }
        });

        // ── О приложении ──────────────────────────────────────────────
        var shareBtn = MakeButton("Поделиться приложением", ThemeColors.Accent);
        shareBtn.Clicked += async (_, _) => await ShareApp();

        var exportBtn = MakeButton("Экспортировать статистику", ThemeColors.Secondary, textColorC: Colors.White);
        exportBtn.Clicked += async (_, _) => await ExportStats();

        var resetBtn = MakeButton("Сбросить прогресс", Colors.Transparent,
            textColorC: ThemeColors.Error, borderColorC: ThemeColors.Error);
        resetBtn.Clicked += async (_, _) => await ResetProgress();

        var versionLabel = new Label
        {
            Text = $"CogniBoost v{SettingsService.AppVersion}",
            FontSize = 13,
            TextColor = ThemeColors.TextMuted,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var aboutSection = BuildSection("О приложении", new VerticalStackLayout
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
                    new Label
                    {
                        Text = "Настройки", FontSize = 28,
                        FontAttributes = FontAttributes.Bold, TextColor = ThemeColors.TextPrimary
                    },
                    accountSection,
                    appearanceSection,
                    accessibilitySection,
                    soundSection,
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
            await DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
            return;
        }

        await DisplayAlertAsync("Готово", "Данные профиля обновлены.", "OK");
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
            "Да, сбросить", "Отмена");
        if (!first) return;

        var second = await DisplayAlertAsync(
            "Подтверждение",
            "Вы уверены? Это действие нельзя отменить.",
            "Удалить всё", "Отмена");
        if (!second) return;

        AccountStore.ResetProgress();
        await DisplayAlertAsync("Готово", "Прогресс сброшен.", "OK");
    }

    // ── Вспомогательные ──────────────────────────────────────────────
    private Border BuildSection(string title, View content)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColors.TextMuted
                    },
                    content
                }
            }
        };
    }

    private static Button MakeButton(string text, Color bg, Color? textColorC = null, Color? borderColorC = null)
    {
        var hasBorder = borderColorC != null && borderColorC != Colors.Transparent;
        return new Button
        {
            Text = text,
            BackgroundColor = bg,
            TextColor = textColorC ?? Colors.White,
            BorderColor = borderColorC ?? Colors.Transparent,
            BorderWidth = hasBorder ? 1.5 : 0,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50,
            CornerRadius = 14
        };
    }

    private View BuildToggleRow(string label, Switch sw)
    {
        var lbl = new Label
        {
            Text = label,
            FontSize = 15,
            TextColor = ThemeColors.TextPrimary,
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
        Grid.SetColumn(sw, 1);
        grid.Children.Add(sw);
        return grid;
    }

    private View BuildLabeledEntry(string label, Entry entry)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = label, FontSize = 12, TextColor = ThemeColors.TextMuted },
                new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = ThemeColors.Border, StrokeThickness = 1,
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
                new Label { Text = label, FontSize = 12, TextColor = ThemeColors.TextMuted },
                new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = ThemeColors.Border, StrokeThickness = 1,
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
                new Label { Text = label, FontSize = 12, TextColor = ThemeColors.TextMuted },
                new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = ThemeColors.Border, StrokeThickness = 1,
                    Padding = new Thickness(12, 4),
                    Content = view
                }
            }
        };
    }
}
