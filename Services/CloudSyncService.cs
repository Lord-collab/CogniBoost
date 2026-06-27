using CogniBoost.Models;

namespace CogniBoost.Services;

public sealed record CloudSyncResult(bool IsSuccess, string Message);

/// <summary>
/// Facade that delegates to SyncService and CloudApiService.
/// Existing callers continue to work; migrate to SyncService directly over time.
/// </summary>
public static class CloudSyncService
{
    public static bool IsEnabled => SyncService.IsEnabled;

    public static CloudSyncResult LastResult
    {
        get => SyncService.LastResult;
        private set { }
    }

    public static Task<CloudSyncResult> SyncCurrentUserAsync()
        => SyncService.SyncCurrentUserAsync();

    public static Task SyncTestResultAsync(TestResult result)
        => SyncService.SyncTestResultAsync(result);

    public static Task<CloudSyncResult> RestorePlayerDataAsync(string username, string password)
        => SyncService.RestorePlayerDataAsync(username, password);
}
