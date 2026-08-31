using Groot.Core.Sessions;
using Groot.Data;
using Groot.Data.Sessions;
using Groot.Data.Users;
using Groot.UI.Health;

namespace Groot.App.Storage;

/// <summary>
/// The phone's own SQLite file, and the one account that uses it. There is no sign-in yet, so
/// the app creates a single local account on first run and every session belongs to it. When
/// accounts and sync arrive this is where the real user comes from instead.
/// </summary>
public sealed class GrootStorage
{
    private const string LocalUsername = "local";

    private readonly SemaphoreSlim _gate = new(1, 1);

    private GrootDatabase? _database;
    private SessionStore? _sessions;
    private SessionMetricsStore? _metrics;
    private Guid _userId;

    /// <summary>
    /// Which device wrote a row. Sync uses it to break a tie between two edits with the same
    /// timestamp, so it has to be stable across restarts and different on each phone.
    /// </summary>
    private static string DeviceId =>
        Preferences.Default.Get("device-id", "") is { Length: > 0 } stored
            ? stored
            : Remember(Guid.NewGuid().ToString("N"));

    private static string Remember(string deviceId)
    {
        Preferences.Default.Set("device-id", deviceId);
        return deviceId;
    }

    /// <summary>
    /// Stores a finished run and everything that was measured during it. Does nothing if the
    /// session has no identity, which is the case for a hand-built session in the gallery.
    /// </summary>
    public async Task SaveRun(MeasuredRun run, DateOnly date)
    {
        if (run.Session is not { } id) return;

        await Ready();

        var sessionId = Guid.NewGuid();
        var updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await _sessions!.Save(
            LoggedSession.Run(sessionId, _userId, date, id.ProgramId, id.Week, id.Day, run.DurationSeconds),
            updatedAt,
            DeviceId);

        foreach (var (monitor, samples) in run.Measured.HeartRate)
            await _metrics!.SaveHeartRate(sessionId, monitor, samples);

        if (run.Measured.Route.Count > 0)
            await _metrics!.SaveRoute(sessionId, run.Measured.Route);
    }

    /// <summary>
    /// Opens the file and finds the account, once. Applying the schema is the one call here that
    /// is not cheap, so a head calls this during startup rather than letting it land inside the
    /// save at the end of a run.
    /// </summary>
    public async Task Ready()
    {
        if (_metrics is not null) return;

        await _gate.WaitAsync();
        try
        {
            if (_metrics is not null) return;

            var database = new GrootDatabase(Path.Combine(FileSystem.AppDataDirectory, "groot.db"));
            var users = new UserStore(database);

            var user = await users.FindByUsername(LocalUsername);
            if (user is null)
            {
                _userId = Guid.NewGuid();
                await users.Save(new StoredUser(_userId, LocalUsername, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            }
            else
            {
                _userId = user.Id;
            }

            _database = database;
            _sessions = new SessionStore(database);
            // Assigned last: it is what the check at the top of this method reads, so nothing
            // can see a half-built storage.
            _metrics = new SessionMetricsStore(database);
        }
        finally
        {
            _gate.Release();
        }
    }
}
