using SQLite;

namespace CogniBoost.Models;

[Table("Users")]
public sealed class UserEntity
{
    [PrimaryKey]
    public string UsernameKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string AvatarEmoji { get; set; } = "\U0001F9E0";
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }
    public string? SkillsJson { get; set; }
    public bool Onboarded { get; set; }
    public int PointsBalance { get; set; }
    public int PointsLifetime { get; set; }
    public string? StreakLastDate { get; set; }
    public int StreakCurrent { get; set; }
    public int StreakLongest { get; set; }
    public string? UnlockedGamesJson { get; set; }
    public string? AchievementsJson { get; set; }
}

[Table("GameHistory")]
public sealed class GameResultEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string UsernameKey { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public int Skill { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int EarnedPoints { get; set; }
    public DateTime PlayedAtUtc { get; set; }
}

[Table("TestHistory")]
public sealed class TestResultEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string UsernameKey { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public int IqScore { get; set; }
    public int EarnedPoints { get; set; }
    public DateTime PlayedAtUtc { get; set; }
}

[Table("Settings")]
public sealed class SettingEntity
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
