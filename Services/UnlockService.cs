using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public static class UnlockService
{
    public static bool IsUnlocked(GameDefinition game)
    {
        if (game.Starter) return true;
        return _unlockedCache?.Contains(game.Id, StringComparer.OrdinalIgnoreCase) ?? false;
    }

    public static bool IsUnlocked(string gameId)
    {
        var game = Pages.GameCatalog.Get(gameId);
        return game is null || IsUnlocked(game);
    }

    public static async Task<(bool Success, string Message)> TryUnlockAsync(GameDefinition game)
    {
        if (IsUnlocked(game))
            return (true, $"«{game.Title}» уже открыта.");

        var (spent, message) = await PointsService.TrySpendAsync(game.UnlockCost);
        if (!spent)
            return (false, message);

        await RefreshCacheAsync();
        _unlockedCache?.Add(game.Id);
        await SaveUnlockedAsync(_unlockedCache ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return (true, $"«{game.Title}» открыта за {game.UnlockCost} ⭐");
    }

    public static async Task RefreshCacheAsync()
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        _unlockedCache = user?.UnlockedGamesJson is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : DeserializeSet(user.UnlockedGamesJson);
    }

    private static HashSet<string>? _unlockedCache;

    private static async Task SaveUnlockedAsync(HashSet<string> unlocked)
    {
        var user = await DatabaseService.Db.FindAsync<UserEntity>(AccountStore.GetCurrentUsernameKey());
        if (user is null) return;

        user.UnlockedGamesJson = JsonSerializer.Serialize(unlocked.ToList());
        await DatabaseService.Db.UpdateAsync(user);
    }

    private static HashSet<string> DeserializeSet(string json)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("[Unlock] Failed to deserialize unlocked games");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
