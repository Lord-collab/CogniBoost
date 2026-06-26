using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public static class AccountStore
{
    private const string IsSignedInKey = "cb_signed_in";
    private const string CurrentUserKey = "cb_current_user";
    private const string GuestModeKey = "cb_guest_mode";

    public const string DefaultAvatar = "\U0001F9E0";

    public static bool HasAccount => GetKnownUsers().Count > 0;

    public static bool IsSignedIn => Preferences.Default.Get(IsSignedInKey, false);

    public static bool IsGuest => !IsSignedIn && Preferences.Default.Get(GuestModeKey, false);

    public static void EnterGuestMode()
    {
        Preferences.Default.Set(GuestModeKey, true);

        DatabaseService.Sync(async () =>
        {
            var existing = await DatabaseService.Db.FindAsync<UserEntity>("guest");
            if (existing is null)
            {
                await DatabaseService.Db.InsertAsync(new UserEntity
                {
                    UsernameKey = "guest",
                    DisplayName = "Гость",
                    AvatarEmoji = "\U0001F464",
                    Onboarded = true
                });
            }
        });
    }

    public static void ExitGuestMode()
    {
        Preferences.Default.Set(GuestModeKey, false);
    }

    public static bool IsCurrentUserOnboarded
    {
        get
        {
            var key = GetCurrentUsernameKey();
            if (string.IsNullOrWhiteSpace(key)) return false;
            return DatabaseService.Sync(async () =>
            {
                var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
                return user?.Onboarded ?? false;
            });
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

        DatabaseService.Sync(async () =>
        {
            var existing = await DatabaseService.Db.FindAsync<UserEntity>(usernameKey);
            if (existing is null)
            {
                await DatabaseService.Db.InsertAsync(new UserEntity
                {
                    UsernameKey = usernameKey,
                    DisplayName = displayUsername,
                    Age = age,
                    AvatarEmoji = DefaultAvatar,
                    PasswordHash = ComputeHash(password),
                });
            }
            else
            {
                existing.DisplayName = displayUsername;
                existing.Age = age;
                existing.PasswordHash = ComputeHash(password);
                await DatabaseService.Db.UpdateAsync(existing);
            }
        });

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
    }

    public static bool TrySignIn(string username, string password, out string error)
    {
        var usernameKey = NormalizeUsername((username ?? string.Empty).Trim());

        var user = DatabaseService.Sync(async () =>
            await DatabaseService.Db.FindAsync<UserEntity>(usernameKey));

        if (user is null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        if (!string.Equals(user.PasswordHash, ComputeHash(password ?? string.Empty), StringComparison.Ordinal))
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

    public static void MigrateGuestData(string displayName, int age, string password)
    {
        var usernameKey = NormalizeUsername(displayName);
        var passwordHash = ComputeHash(password);

        DatabaseService.Sync(async () =>
        {
            var guest = await DatabaseService.Db.FindAsync<UserEntity>("guest");
            if (guest is not null)
            {
                await DatabaseService.Db.DeleteAsync(guest);
                guest.UsernameKey = usernameKey;
                guest.DisplayName = displayName.Trim();
                guest.Age = age;
                guest.PasswordHash = passwordHash;
                guest.Onboarded = true;
                await DatabaseService.Db.InsertAsync(guest);
            }

            await DatabaseService.Db.ExecuteAsync(
                "UPDATE GameHistory SET UsernameKey = ? WHERE UsernameKey = 'guest'", usernameKey);
            await DatabaseService.Db.ExecuteAsync(
                "UPDATE TestHistory SET UsernameKey = ? WHERE UsernameKey = 'guest'", usernameKey);
        });

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
        ExitGuestMode();
    }

    public static bool TryGetCurrentProfile(out UserProfile profile)
    {
        profile = new UserProfile();
        if (!IsSignedIn) return false;

        var usernameKey = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(usernameKey)) return false;

        var user = DatabaseService.Sync(async () =>
            await DatabaseService.Db.FindAsync<UserEntity>(usernameKey));

        if (user is null) return false;

        profile = new UserProfile
        {
            Username = user.DisplayName,
            Age = user.Age,
            AvatarEmoji = user.AvatarEmoji,
            SelectedSkills = LoadSkills(user.SkillsJson)
        };
        return true;
    }

    public static string GetCurrentUsernameKey()
        => Preferences.Default.Get(CurrentUserKey, string.Empty);

    public static void SaveAvatar(string emoji)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(emoji))
            return;

        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
            if (user is not null)
            {
                user.AvatarEmoji = emoji.Trim();
                await DatabaseService.Db.UpdateAsync(user);
            }
        });
    }

    public static void SaveSelectedSkills(IEnumerable<BrainSkill> skills)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key))
            return;

        var ids = skills.Distinct().Select(s => (int)s).ToList();
        var json = JsonSerializer.Serialize(ids);

        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
            if (user is not null)
            {
                user.SkillsJson = json;
                user.Onboarded = true;
                await DatabaseService.Db.UpdateAsync(user);
            }
        });
    }

    public static bool TryUpdateDisplayName(string newName, out string error)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) { error = "Нет авторизованного пользователя."; return false; }
        var name = (newName ?? string.Empty).Trim();
        if (name.Length < 3) { error = "Имя должно содержать не менее 3 символов."; return false; }

        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
            if (user is not null)
            {
                user.DisplayName = name;
                await DatabaseService.Db.UpdateAsync(user);
            }
        });

        error = string.Empty;
        return true;
    }

    public static bool TryUpdateAge(string ageText, out string error)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) { error = "Нет авторизованного пользователя."; return false; }
        if (!int.TryParse((ageText ?? string.Empty).Trim(), out var age) || age is < 8 or > 99)
        { error = "Введите корректный возраст от 8 до 99."; return false; }

        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
            if (user is not null)
            {
                user.Age = age;
                await DatabaseService.Db.UpdateAsync(user);
            }
        });

        error = string.Empty;
        return true;
    }

    public static bool TryChangePassword(string oldPassword, string newPassword, string confirmPassword, out string error)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) { error = "Нет авторизованного пользователя."; return false; }

        var user = DatabaseService.Sync(async () =>
            await DatabaseService.Db.FindAsync<UserEntity>(key));

        if (user is null) { error = "Пользователь не найден."; return false; }

        if (!string.Equals(user.PasswordHash, ComputeHash(oldPassword ?? string.Empty), StringComparison.Ordinal))
        { error = "Неверный текущий пароль."; return false; }

        if ((newPassword ?? string.Empty).Length < 6 || !newPassword!.Any(char.IsDigit))
        { error = "Новый пароль: не короче 6 символов и должен содержать цифру."; return false; }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        { error = "Новые пароли не совпадают."; return false; }

        DatabaseService.Sync(async () =>
        {
            user.PasswordHash = ComputeHash(newPassword ?? string.Empty);
            await DatabaseService.Db.UpdateAsync(user);
        });

        error = string.Empty;
        return true;
    }

    public static void ResetOnboarding()
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return;

        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
            if (user is not null)
            {
                user.Onboarded = false;
                await DatabaseService.Db.UpdateAsync(user);
            }
        });
    }

    public static void ResetProgress()
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return;

        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
            if (user is not null)
            {
                user.PointsBalance = 0;
                user.PointsLifetime = 0;
                user.StreakLastDate = null;
                user.StreakCurrent = 0;
                user.StreakLongest = 0;
                user.UnlockedGamesJson = null;
                user.AchievementsJson = null;
                await DatabaseService.Db.UpdateAsync(user);
            }

            await DatabaseService.Db.ExecuteAsync(
                "DELETE FROM GameHistory WHERE UsernameKey = ?", key);
            await DatabaseService.Db.ExecuteAsync(
                "DELETE FROM TestHistory WHERE UsernameKey = ?", key);
        });
    }

    // ---------------------------------------------------------------
    // Приватные методы
    // ---------------------------------------------------------------

    private static List<string> GetKnownUsers()
    {
        return DatabaseService.Sync(async () =>
        {
            var entities = await DatabaseService.Db
                .Table<UserEntity>()
                .ToListAsync();
            return entities.Select(u => u.UsernameKey).ToList();
        });
    }

    private static List<BrainSkill> LoadSkills(string? skillsJson)
    {
        if (string.IsNullOrWhiteSpace(skillsJson))
            return new List<BrainSkill>();

        try
        {
            var ids = JsonSerializer.Deserialize<List<int>>(skillsJson) ?? new List<int>();
            return ids.Select(i => (BrainSkill)i).Distinct().ToList();
        }
        catch
        {
            return new List<BrainSkill>();
        }
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
