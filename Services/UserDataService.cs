using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public static class UserDataService
{
    public static async Task<bool> IsCurrentUserOnboardedAsync()
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return false;
        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
        return user?.Onboarded ?? false;
    }

    public static async Task<bool> TryGetCurrentProfileAsync()
    {
        if (!AuthService.IsSignedIn) return false;

        var usernameKey = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(usernameKey)) return false;

        return await DatabaseService.Db.FindAsync<UserEntity>(usernameKey) is not null;
    }

    public static async Task<UserProfile?> GetProfileAsync()
    {
        if (!AuthService.IsSignedIn) return null;

        var usernameKey = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(usernameKey)) return null;

        var user = await DatabaseService.Db.FindAsync<UserEntity>(usernameKey);
        if (user is null) return null;

        return new UserProfile
        {
            Username = user.DisplayName,
            Age = user.Age,
            AvatarEmoji = user.AvatarEmoji,
            SelectedSkills = LoadSkills(user.SkillsJson)
        };
    }

    public static async Task SaveAvatarAsync(string emoji)
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(emoji))
            return;

        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
        if (user is not null)
        {
            user.AvatarEmoji = emoji.Trim();
            await DatabaseService.Db.UpdateAsync(user);
        }
    }

    public static async Task SaveSelectedSkillsAsync(IEnumerable<BrainSkill> skills)
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key))
            return;

        var ids = skills.Distinct().Select(s => (int)s).ToList();
        var json = JsonSerializer.Serialize(ids);

        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
        if (user is not null)
        {
            user.SkillsJson = json;
            user.Onboarded = true;
            await DatabaseService.Db.UpdateAsync(user);
        }
    }

    public static async Task<(bool Success, string Error)> TryUpdateDisplayNameAsync(string newName)
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return (false, "Нет авторизованного пользователя.");
        var name = (newName ?? string.Empty).Trim();
        if (name.Length < 3) return (false, "Имя должно содержать не менее 3 символов.");

        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
        if (user is not null)
        {
            user.DisplayName = name;
            await DatabaseService.Db.UpdateAsync(user);
        }

        return (true, string.Empty);
    }

    public static async Task<(bool Success, string Error)> TryUpdateAgeAsync(string ageText)
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return (false, "Нет авторизованного пользователя.");
        if (!int.TryParse((ageText ?? string.Empty).Trim(), out var age) || age is < 8 or > 99)
            return (false, "Введите корректный возраст от 8 до 99.");

        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
        if (user is not null)
        {
            user.Age = age;
            await DatabaseService.Db.UpdateAsync(user);
        }

        return (true, string.Empty);
    }

    public static async Task ResetOnboardingAsync()
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return;

        var user = await DatabaseService.Db.FindAsync<UserEntity>(key);
        if (user is not null)
        {
            user.Onboarded = false;
            await DatabaseService.Db.UpdateAsync(user);
        }
    }

    public static async Task ResetProgressAsync()
    {
        var key = AuthService.GetCurrentUsernameKey();
        if (string.IsNullOrWhiteSpace(key)) return;

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
    }

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

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
            System.Diagnostics.Debug.WriteLine("[UserData] Failed to load skills");
            return new List<BrainSkill>();
        }
    }
}
