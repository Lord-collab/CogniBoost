using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

public static class UnlockService
{
    public static bool IsUnlocked(GameDefinition game)
    {
        if (game.Starter) return true;
        return LoadUnlocked().Contains(game.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsUnlocked(string gameId)
    {
        var game = Pages.GameCatalog.Get(gameId);
        return game is null || IsUnlocked(game);
    }

    public static bool TryUnlock(GameDefinition game, out string message)
    {
        if (IsUnlocked(game))
        {
            message = $"«{game.Title}» уже открыта.";
            return true;
        }

        if (!PointsService.TrySpend(game.UnlockCost, out message))
            return false;

        var unlocked = LoadUnlocked();
        unlocked.Add(game.Id);
        SaveUnlocked(unlocked);
        message = $"«{game.Title}» открыта за {game.UnlockCost} ⭐";
        return true;
    }

    private static HashSet<string> LoadUnlocked()
    {
        return DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            if (user?.UnlockedGamesJson is null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(user.UnlockedGamesJson)
                           ?? new List<string>();
                return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        });
    }

    private static void SaveUnlocked(HashSet<string> unlocked)
    {
        DatabaseService.Sync(async () =>
        {
            var user = await DatabaseService.Db.FindAsync<UserEntity>(UserKey());
            if (user is null) return;

            user.UnlockedGamesJson = JsonSerializer.Serialize(unlocked.ToList());
            await DatabaseService.Db.UpdateAsync(user);
        });
    }

    private static string UserKey()
        => AccountStore.GetCurrentUsernameKey();
}
