using CogniBoost.Models;
using CogniBoost.Services;

namespace CogniBoost.Pages;

/// <summary>
/// Базовый класс для мини-игр.
/// Завершение сессии: запись результата → streak → очки → достижения → результат.
/// </summary>
public abstract class GameBasePage : ContentPage
{
    protected GameBasePage(GameDefinition definition)
    {
        Definition = definition;
        Title = definition.Title;
        BackgroundColor = ThemeColors.PageBg;
    }

    protected GameDefinition Definition { get; }

    protected async Task FinishAsync(int score, int maxScore)
    {
        var accuracy = maxScore > 0 ? Math.Clamp(score / (double)maxScore, 0, 1) : 0;
        var earned   = PointsService.AwardForResult(accuracy);

        HapticService.Success();
        SoundService.PlayComplete();

        // Daily challenge: удвоенные бонусы
        var isChallenge = DailyChallengeService.IsChallengeGame(Definition.Id);
        if (isChallenge) earned *= (int)DailyChallengeService.BonusMultiplier;

        var result = new GameResult(
            GameId:       Definition.Id,
            GameTitle:    Definition.Title,
            Skill:        Definition.Skill,
            Score:        score,
            MaxScore:     maxScore,
            EarnedPoints: earned,
            PlayedAtUtc:  DateTime.UtcNow);

        await ProgressStore.AddGameResultAsync(result);

        // Streak
        var (streak, streakBonus) = await StreakService.RecordActivityAsync();

        // Достижения
        var newAchievements = await AchievementsService.CheckAndUnlockAsync();

        // Попап новых достижений
        if (newAchievements.Count > 0)
            await AchievementPopupService.ShowAsync(this, newAchievements);

        // Синхронизация в фоне
        _ = CloudSyncService.SyncCurrentUserAsync();

        var resultPage = new GameResultPage(result, streak, streakBonus, newAchievements);
        await Navigation.PushAsync(resultPage);
    }
}
