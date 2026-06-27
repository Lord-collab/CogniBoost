using System.Security.Cryptography;
using System.Text;
using CogniBoost.Models;

namespace CogniBoost.Services;

public static class AuthService
{
    private const string IsSignedInKey = "cb_signed_in";
    private const string CurrentUserKey = "cb_current_user";
    private const string GuestModeKey = "cb_guest_mode";

    public const string DefaultAvatar = "\U0001F9E0";

    public static bool IsSignedIn => Preferences.Default.Get(IsSignedInKey, false);

    public static bool IsGuest => !IsSignedIn && Preferences.Default.Get(GuestModeKey, false);

    public static async Task<bool> HasAccountAsync()
        => (await GetKnownUsersAsync()).Count > 0;

    public static async Task EnterGuestModeAsync()
    {
        Preferences.Default.Set(GuestModeKey, true);
        Preferences.Default.Set(CurrentUserKey, "guest");

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
    }

    public static void ExitGuestMode()
    {
        Preferences.Default.Set(GuestModeKey, false);
        Preferences.Default.Set(CurrentUserKey, string.Empty);
    }

    public static string GetCurrentUsernameKey()
    {
        var key = Preferences.Default.Get(CurrentUserKey, string.Empty);
        return string.IsNullOrWhiteSpace(key) ? "guest" : key;
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
        if (Task.Run(() => GetKnownUsersAsync()).GetAwaiter().GetResult().Contains(usernameKey))
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

    public static async Task SaveAccountAsync(string username, int age, string password)
    {
        var displayUsername = username.Trim();
        var usernameKey = NormalizeUsername(displayUsername);

        var existing = await DatabaseService.Db.FindAsync<UserEntity>(usernameKey);
        var salt = GenerateSalt();
        if (existing is null)
        {
            await DatabaseService.Db.InsertAsync(new UserEntity
            {
                UsernameKey = usernameKey,
                DisplayName = displayUsername,
                Age = age,
                AvatarEmoji = DefaultAvatar,
                PasswordHash = ComputeHash(password, salt),
                PasswordSalt = salt,
            });
        }
        else
        {
            existing.DisplayName = displayUsername;
            existing.Age = age;
            existing.PasswordHash = ComputeHash(password, salt);
            existing.PasswordSalt = salt;
            await DatabaseService.Db.UpdateAsync(existing);
        }

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
    }

    public static async Task<(bool Success, string Error)> TrySignInAsync(string username, string password)
    {
        var usernameKey = NormalizeUsername((username ?? string.Empty).Trim());

        var user = await DatabaseService.Db.FindAsync<UserEntity>(usernameKey);

        if (user is null)
            return (false, "Пользователь не найден.");

        if (!string.Equals(user.PasswordHash, ComputeHash(password ?? string.Empty, user.PasswordSalt), StringComparison.Ordinal))
        {
            if (string.IsNullOrEmpty(user.PasswordSalt) &&
                string.Equals(user.PasswordHash, ComputeHash(password ?? string.Empty), StringComparison.Ordinal))
            {
                user.PasswordSalt = GenerateSalt();
                user.PasswordHash = ComputeHash(password ?? string.Empty, user.PasswordSalt);
                await DatabaseService.Db.UpdateAsync(user);
            }
            else
                return (false, "Неверный пароль.");
        }

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
        return (true, string.Empty);
    }

    public static void SignOut()
    {
        Preferences.Default.Set(IsSignedInKey, false);
        Preferences.Default.Set(CurrentUserKey, string.Empty);
    }

    public static async Task MigrateGuestDataAsync(string displayName, int age, string password)
    {
        var usernameKey = NormalizeUsername(displayName);
        var salt = GenerateSalt();
        var passwordHash = ComputeHash(password, salt);

        var guest = await DatabaseService.Db.FindAsync<UserEntity>("guest");
        if (guest is not null)
        {
            await DatabaseService.Db.DeleteAsync(guest);
            guest.UsernameKey = usernameKey;
            guest.DisplayName = displayName.Trim();
            guest.Age = age;
            guest.PasswordHash = passwordHash;
            guest.PasswordSalt = salt;
            guest.Onboarded = true;
            await DatabaseService.Db.InsertAsync(guest);
        }

        await DatabaseService.Db.ExecuteAsync(
            "UPDATE GameHistory SET UsernameKey = ? WHERE UsernameKey = 'guest'", usernameKey);
        await DatabaseService.Db.ExecuteAsync(
            "UPDATE TestHistory SET UsernameKey = ? WHERE UsernameKey = 'guest'", usernameKey);

        Preferences.Default.Set(CurrentUserKey, usernameKey);
        Preferences.Default.Set(IsSignedInKey, true);
        ExitGuestMode();
    }

    public static async Task<(bool Success, string Error)> TryChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)
    {
        var key = GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return (false, "Нет авторизованного пользователя.");

        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);

        if (user is null) return (false, "Пользователь не найден.");

        if (!string.Equals(user.PasswordHash, ComputeHash(oldPassword ?? string.Empty, user.PasswordSalt), StringComparison.Ordinal))
        {
            if (string.IsNullOrEmpty(user.PasswordSalt) &&
                string.Equals(user.PasswordHash, ComputeHash(oldPassword ?? string.Empty), StringComparison.Ordinal))
            {
            }
            else
                return (false, "Неверный текущий пароль.");
        }

        if ((newPassword ?? string.Empty).Length < 6 || !newPassword!.Any(char.IsDigit))
            return (false, "Новый пароль: не короче 6 символов и должен содержать цифру.");

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            return (false, "Новые пароли не совпадают.");

        var newSalt = GenerateSalt();
        user.PasswordHash = ComputeHash(newPassword ?? string.Empty, newSalt);
        user.PasswordSalt = newSalt;
        await DatabaseService.Db.UpdateAsync(user);

        return (true, string.Empty);
    }

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    private static async Task<List<string>> GetKnownUsersAsync()
    {
        var entities = await DatabaseService.Db
            .Table<UserEntity>()
            .ToListAsync();
        return entities.Select(u => u.UsernameKey).ToList();
    }

    private static string ComputeHash(string password, string? salt = null)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        if (!string.IsNullOrEmpty(salt))
        {
            var salted = Encoding.UTF8.GetBytes(salt).Concat(bytes).ToArray();
            return Convert.ToHexString(SHA256.HashData(salted));
        }
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string GenerateSalt()
    {
        var salt = new byte[32];
        RandomNumberGenerator.Fill(salt);
        return Convert.ToHexString(salt);
    }

    private static string NormalizeUsername(string username)
        => (username ?? string.Empty).Trim().ToLowerInvariant();
}
