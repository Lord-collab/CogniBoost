namespace CogniBoost.Models;

/// <summary>
/// Профиль пользователя приложения.
/// </summary>
public sealed class UserProfile
{
    public string Username { get; set; } = string.Empty;
    public int Age { get; set; }
    public string AvatarEmoji { get; set; } = "\U0001F9E0";
    public List<BrainSkill> SelectedSkills { get; set; } = new();
}
