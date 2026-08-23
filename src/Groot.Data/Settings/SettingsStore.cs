using Dapper;

namespace Groot.Data.Settings;

/// <summary>
/// The handful of choices the contract maths reads: how many jokers a week allows, and which day
/// a week starts on. The week start is a setting rather than ISO-8601, because the answer differs
/// by locale and the lifter may disagree with theirs.
/// </summary>
public sealed record UserSettings(int JokersPerWeek, DayOfWeek WeekStartDay)
{
    /// <summary>Two jokers, and the week starting on the device locale's first day.</summary>
    public static UserSettings Default(DayOfWeek localeFirstDay) => new(2, localeFirstDay);
}

/// <summary>Reads and writes the one settings row an account has.</summary>
public sealed class SettingsStore(GrootDatabase database)
{
    private const string SaveSettings = """
        INSERT INTO settings (user_id, jokers_per_week, week_start_day, updated_at, device_id)
        VALUES (@UserId, @JokersPerWeek, @WeekStartDay, @UpdatedAt, @DeviceId)
        ON CONFLICT(user_id) DO UPDATE SET
            jokers_per_week = excluded.jokers_per_week,
            week_start_day  = excluded.week_start_day,
            updated_at      = excluded.updated_at,
            device_id       = excluded.device_id
        """;

    private const string MergeSettings = $"""
        {SaveSettings}
        WHERE (excluded.updated_at, excluded.device_id) > (settings.updated_at, settings.device_id)
        """;

    private const string Select =
        "SELECT jokers_per_week, week_start_day FROM settings WHERE user_id = @userId";

    /// <summary>A change made on this device. Unconditional, as every local write is.</summary>
    public Task Save(Guid userId, UserSettings settings, long updatedAt, string deviceId, CancellationToken cancellationToken = default) =>
        Write(SaveSettings, userId, settings, updatedAt, deviceId, cancellationToken);

    /// <summary>Settings arriving from the server. Last write wins, ties broken by device.</summary>
    public Task Merge(Guid userId, UserSettings settings, long updatedAt, string deviceId, CancellationToken cancellationToken = default) =>
        Write(MergeSettings, userId, settings, updatedAt, deviceId, cancellationToken);

    private async Task Write(string statement, Guid userId, UserSettings settings, long updatedAt, string deviceId, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        await connection.ExecuteAsync(new CommandDefinition(statement, new
        {
            UserId = SqliteValues.FromGuid(userId),
            settings.JokersPerWeek,
            WeekStartDay = (int)settings.WeekStartDay,
            UpdatedAt = updatedAt,
            DeviceId = deviceId,
        }, cancellationToken: cancellationToken));
    }

    /// <summary>The stored settings, or null when the account has never saved any.</summary>
    public async Task<UserSettings?> Find(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        var row = await connection.QuerySingleOrDefaultAsync<SettingsRow>(new CommandDefinition(Select, new { userId = SqliteValues.FromGuid(userId) }, cancellationToken: cancellationToken));

        return row is null ? null : new UserSettings((int)row.JokersPerWeek, (DayOfWeek)row.WeekStartDay);
    }

    private sealed record SettingsRow(long JokersPerWeek, long WeekStartDay);
}
