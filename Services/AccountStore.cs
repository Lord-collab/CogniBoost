using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Локальное хранилище аккаунтов на основе Preferences.
/// Пароли хранятся в виде SHA256-хеша. На этапе 8 будет добавлена
/// синхронизация с Supabase через общий слой данных.
/// </summary>
public static class AccountStore
{
    private const string AccountsKey = "cb_accounts";
    private const string IsSignedInKey = "cb_signed_in";
    private const string CurrentUserKey = "cb_current_user";

    private const string DisplayNamePrefix = "cb_display_";
    private const string AgePrefix = "cb_age_";
    private const string PasswordHashPrefix = "cb_pwd_";
    private const string AvatarPrefix = "cb_avatar_";
    private const string SkillsPrefix = "cb_skills_";
    private const string OnboardedPrefix = "cb_onboarded_";

    public const string DefaultAvatar = "\U0001F9E0";

    public static bool HasAccount => GetKnownUsers().Count > 0;

    public static bool IsSignedIn => Preferences.Default.Get(IsSignedInKey, false);

    /// <summary>Завершён ли онбординг (выбор направлений) для текущего пользователя.</summary>
    public static bool IsCurrentUserOnboarded
    {
        get
        {
            var key = NormalizeUsername(Preferences.Default.Get(CurrentUserKey, string.Empty));
            return !string.IsNullOrWhiteSpace(key)
                && Preferences.Default.Get($"{OnboardedPrefix}{key}", false);
        }
    }

    public static bool TryValidateRegistration(
        string username,
        string ageText,
        string password,
        string confirmPassword,
        out int age,
        out string error)
    {
        age = 0;
        var displayUsername = (username ?? string.Empty).Trim();

        if (displayUsername.Length < 3)
        {
            error = "Имя пользователя должно содержать не менее 3 символов.";
            return false;
        }

        var usernameKey = NormalizeUsername(displayUsername);
        if (GetKnownUsers().Contains(usernameKey))
        {
            error = "Такое имя уже занято. Выберите другое.";
            return false;
        }

        if (!int.TryParse((ageText ?? string.Empty).Trim(), out age) || age is < 8 or > 99)
        {
            error = "Введите корректный возраст от 8 до 99.";
            return false;
        }

        if ((password ?? string.Empty).Length < 6 || !password!.Any(char.IsDigit))
        {
            error = "Пароль должен быть не короче 6 символов и содержать цифру.";
            return false;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            error = "Пароли не совпадают.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void SaveAccount(string username, int age, string password)
    {
        var displayUsername = username.Trim();
        var usernameKey = NormalizeUsername(displayUsername);

        var users = GetKnownUsers();
        if (!users.Contains(usernameKey))
        {
            users.Add(usernameKey);
            SaveKnownUsers(users);
        }

        Preferences.Default.Set($"{DisplayNamePrefix}{usernameKey}", displayUsername);
        Preferences.Default.Set($"{AgePrefix}{usernameKey}", age);
        Preferences.Default.Set($"{PasswordHashPrefix}{usernameKey}", ComputeHash(password));

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
    }

    public static bool TrySignIn(string username, string password, out string error)
    {
        var usernameKey = NormalizeUsername((username ?? string.Empty).Trim());
        if (!GetKnownUsers().Contains(usernameKey))
        {
            error = "Пользователь не найден.";
            return false;
        }

        var storedHash = Preferences.Default.Get($"{PasswordHashPrefix}{usernameKey}", string.Empty);
        if (storedHash.Length == 0 || !string.Equals(storedHash, ComputeHash(password ?? string.Empty), StringComparison.Ordinal))
        {
            error = "Неверный пароль.";
            return false;
        }

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
        error = string.Empty;
        return true;
    }

    public static void SignOut()
    {
        Preferences.Default.Set(IsSignedInKey, false);
    }

    public static bool TryGetCurrentProfile(out UserProfile profile)
    {
        profile = new UserProfile();
        if (!IsSignedIn)
        {
            return false;
        }

        var usernameKey = NormalizeUsername(Preferences.Default.Get(CurrentUserKey, string.Empty));
        if (string.IsNullOrWhiteSpace(usernameKey) || !GetKnownUsers().Contains(usernameKey))
        {
            return false;
        }

        var displayName = Preferences.Default.Get($"{DisplayNamePrefix}{usernameKey}", usernameKey);
        profile = new UserProfile
        {
            Username = string.IsNullOrWhiteSpace(displayName) ? usernameKey : displayName,
            Age = Preferences.Default.Get($"{AgePrefix}{usernameKey}", 0),
            AvatarEmoji = Preferences.Default.Get($"{AvatarPrefix}{usernameKey}", DefaultAvatar),
            SelectedSkills = LoadSkills(usernameKey)
        };
        return true;
    }

    public static string GetCurrentUsernameKey()
        => NormalizeUsername(Preferences.Default.Get(CurrentUserKey, string.Empty));

    public static void SaveAvatar(string emoji)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(emoji))
        {
            return;
        }

        Preferences.Default.Set($"{AvatarPrefix}{key}", emoji.Trim());
    }

    public static void SaveSelectedSkills(IEnumerable<BrainSkill> skills)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var ids = skills.Distinct().Select(s => (int)s).ToList();
        Preferences.Default.Set($"{SkillsPrefix}{key}", JsonSerializer.Serialize(ids));
        Preferences.Default.Set($"{OnboardedPrefix}{key}", true);
    }

    public static bool TryUpdateDisplayName(string newName, out string error)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) { error = "Нет авторизованного пользователя."; return false; }
        var name = (newName ?? string.Empty).Trim();
        if (name.Length < 3) { error = "Имя должно содержать не менее 3 символов."; return false; }
        Preferences.Default.Set($"{DisplayNamePrefix}{key}", name);
        error = string.Empty;
        return true;
    }

    public static bool TryUpdateAge(string ageText, out string error)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) { error = "Нет авторизованного пользователя."; return false; }
        if (!int.TryParse((ageText ?? string.Empty).Trim(), out var age) || age is < 8 or > 99)
        { error = "Введите корректный возраст от 8 до 99."; return false; }
        Preferences.Default.Set($"{AgePrefix}{key}", age);
        error = string.Empty;
        return true;
    }

    public static bool TryChangePassword(string oldPassword, string newPassword, string confirmPassword, out string error)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) { error = "Нет авторизованного пользователя."; return false; }
        var storedHash = Preferences.Default.Get($"{PasswordHashPrefix}{key}", string.Empty);
        if (!string.Equals(storedHash, ComputeHash(oldPassword ?? string.Empty), StringComparison.Ordinal))
        { error = "Неверный текущий пароль."; return false; }
        if ((newPassword ?? string.Empty).Length < 6 || !newPassword!.Any(char.IsDigit))
        { error = "Новый пароль: не короче 6 символов и должен содержать цифру."; return false; }
        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        { error = "Новые пароли не совпадают."; return false; }
        Preferences.Default.Set($"{PasswordHashPrefix}{key}", ComputeHash(newPassword ?? string.Empty));
        error = string.Empty;
        return true;
    }

    public static void ResetOnboarding()
    {
        var key = GetCurrentUsernameKey();
        if (!string.IsNullOrWhiteSpace(key))
            Preferences.Default.Set($"{OnboardedPrefix}{key}", false);
    }

    public static void ResetProgress()
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return;

        var prefixes = new[]
        {
            "cb_game_history_", "cb_test_history_",
            "cb_points_balance_", "cb_points_lifetime_",
            "cb_streak_last_", "cb_streak_current_", "cb_streak_longest_",
            "cb_unlocked_games_",
        };
        foreach (var prefix in prefixes)
            Preferences.Default.Remove($"{prefix}{key}");

        var achIds = new[]
        {
            "first_game","games_5","games_10","perfect_score","accuracy_90","unlock_game","unlock_all",
            "first_test","iq_100","iq_120","iq_130","tests_5",
            "brain_200","brain_500","brain_800","points_500","points_1000",
            "streak_3","streak_7","streak_30",
            "skill_memory_500","skill_focus_500","skill_lang_500","skill_logic_500",
        };
        foreach (var id in achIds)
        {
            Preferences.Default.Remove($"cb_ach_u_{key}_{id}");
            Preferences.Default.Remove($"cb_ach_t_{key}_{id}");
        }
    }

    private static List<BrainSkill> LoadSkills(string usernameKey)
    {
        var raw = Preferences.Default.Get($"{SkillsPrefix}{usernameKey}", string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<BrainSkill>();
        }

        try
        {
            var ids = JsonSerializer.Deserialize<List<int>>(raw) ?? new List<int>();
            return ids.Select(i => (BrainSkill)i).Distinct().ToList();
        }
        catch
        {
            return new List<BrainSkill>();
        }
    }

    private static List<string> GetKnownUsers()
    {
        var json = Preferences.Default.Get(AccountsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return (JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>())
                .Select(NormalizeUsername)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static void SaveKnownUsers(IEnumerable<string> users)
    {
        var normalized = users
            .Select(NormalizeUsername)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Preferences.Default.Set(AccountsKey, JsonSerializer.Serialize(normalized));
    }

    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string NormalizeUsername(string username)
        => (username ?? string.Empty).Trim().ToLowerInvariant();
}
