using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Фасад для AuthService + UserDataService.
/// Упрощает доступ к аутентификации и профилю — один класс вместо двух
/// для всех страниц приложения. При рефакторинге вызывайте сервисы напрямую.
/// </summary>
public static class AccountStore
{
    // ── Auth ──────────────────────────────────────────────────────
    public static bool IsSignedIn => AuthService.IsSignedIn;
    public static bool IsGuest => AuthService.IsGuest;
    public const string DefaultAvatar = "\U0001F9E0";

    public static Task EnterGuestModeAsync() => AuthService.EnterGuestModeAsync();
    public static void ExitGuestMode() => AuthService.ExitGuestMode();
    public static string GetCurrentUsernameKey() => AuthService.GetCurrentUsernameKey();
    public static bool TryValidateRegistration(string username, string ageText, string password, string confirmPassword, out int age, out string error)
        => AuthService.TryValidateRegistration(username, ageText, password, confirmPassword, out age, out error);
    public static Task<string?> GetPasswordHintAsync(string username)
        => AuthService.GetPasswordHintAsync(username);
    public static Task SaveAccountAsync(string username, int age, string password, string? hint = null)
        => AuthService.SaveAccountAsync(username, age, password, hint);
    public static Task<(bool Success, string Error)> TrySignInAsync(string username, string password)
        => AuthService.TrySignInAsync(username, password);
    public static void SignOut() => AuthService.SignOut();
    public static Task MigrateGuestDataAsync(string displayName, int age, string password, string? hint = null)
        => AuthService.MigrateGuestDataAsync(displayName, age, password, hint);
    public static Task<(bool Success, string Error)> TryChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)
        => AuthService.TryChangePasswordAsync(oldPassword, newPassword, confirmPassword);

    // ── User Data ─────────────────────────────────────────────────
    public static Task<bool> IsCurrentUserOnboardedAsync() => UserDataService.IsCurrentUserOnboardedAsync();
    public static Task<UserProfile?> GetProfileAsync() => UserDataService.GetProfileAsync();
    public static Task SaveAvatarAsync(string emoji) => UserDataService.SaveAvatarAsync(emoji);
    public static Task SaveSelectedSkillsAsync(IEnumerable<BrainSkill> skills) => UserDataService.SaveSelectedSkillsAsync(skills);
    public static Task<(bool Success, string Error)> TryUpdateDisplayNameAsync(string newName)
        => UserDataService.TryUpdateDisplayNameAsync(newName);
    public static Task<(bool Success, string Error)> TryUpdateAgeAsync(string ageText)
        => UserDataService.TryUpdateAgeAsync(ageText);
    public static Task ResetOnboardingAsync() => UserDataService.ResetOnboardingAsync();
    public static Task ResetProgressAsync() => UserDataService.ResetProgressAsync();
    public static Task<bool> HasAccountAsync() => AuthService.HasAccountAsync();
}
