using Dapper;
using Groot.Core.Health;

namespace Groot.Data.Sessions;

/// <summary>
/// What a device measured during a session: heart rate readings and position fixes. Both are
/// children of one session and are written as a set, the same way sets are, because a run's
/// measurements are not edited afterwards — they are recorded once and replaced only if the
/// session is recorded again.
/// <para>
/// Not scoped by account, unlike <see cref="SessionStore"/>: these rows hang off a session id
/// that is already scoped, and the foreign key will not let a measurement exist without one.
/// </para>
/// </summary>
public sealed class SessionMetricsStore(GrootDatabase database)
{
    /// <summary>
    /// Replaces every heart rate reading recorded for a session. Readings the engine would refuse
    /// are dropped here too, so a monitor that reports a spike on a loose strap cannot land in
    /// the file and fail the CHECK on the way in.
    /// </summary>
    public async Task SaveHeartRate(
        Guid sessionId,
        string sourceId,
        IEnumerable<HeartRateSample> samples,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        var rows = samples
            .Where(sample => sample.IsPlausible)
            // The primary key is (session, source, second) and a monitor reporting twice inside
            // one second is ordinary, so the last reading of each second wins rather than the
            // insert failing.
            .GroupBy(sample => sample.ElapsedSeconds)
            .Select(group => new
            {
                SessionId = SqliteValues.FromGuid(sessionId),
                SourceId = sourceId,
                ElapsedS = (long)group.Key,
                Bpm = (long)group.Last().Bpm,
            })
            .ToArray();

        using var connection = database.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            SessionMetricsStatements.DeleteHeartRateOfSource,
            new { sessionId = SqliteValues.FromGuid(sessionId), sourceId },
            transaction,
            cancellationToken: cancellationToken));

        if (rows.Length > 0)
            await connection.ExecuteAsync(new CommandDefinition(
                SessionMetricsStatements.InsertHeartRate, rows, transaction, cancellationToken: cancellationToken));

        transaction.Commit();
    }

    /// <summary>Replaces the route recorded for a session. Unusable fixes are dropped.</summary>
    public async Task SaveRoute(
        Guid sessionId,
        IEnumerable<RouteFix> fixes,
        CancellationToken cancellationToken = default)
    {
        var rows = fixes
            .Where(fix => fix.IsUsable)
            .GroupBy(fix => fix.ElapsedSeconds)
            .Select(group => group.Last())
            .Select(fix => new
            {
                SessionId = SqliteValues.FromGuid(sessionId),
                ElapsedS = (long)fix.ElapsedSeconds,
                LatE7 = GeoValues.ToE7(fix.Latitude),
                LonE7 = GeoValues.ToE7(fix.Longitude),
                AccuracyCm = GeoValues.ToCentimetres(fix.AccuracyMetres),
                Bpm = (long?)fix.Bpm,
            })
            .ToArray();

        using var connection = database.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            SessionMetricsStatements.DeleteRoute,
            new { sessionId = SqliteValues.FromGuid(sessionId) },
            transaction,
            cancellationToken: cancellationToken));

        if (rows.Length > 0)
            await connection.ExecuteAsync(new CommandDefinition(
                SessionMetricsStatements.InsertRouteFix, rows, transaction, cancellationToken: cancellationToken));

        transaction.Commit();
    }

    /// <summary>Every heart rate reading recorded for a session by one monitor, oldest first.</summary>
    public async Task<IReadOnlyList<HeartRateSample>> ReadHeartRate(
        Guid sessionId,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var rows = await connection.QueryAsync<HeartRateRow>(new CommandDefinition(
            SessionMetricsStatements.SelectHeartRateOfSource,
            new { sessionId = SqliteValues.FromGuid(sessionId), sourceId },
            cancellationToken: cancellationToken));

        return rows.Select(row => new HeartRateSample((int)row.ElapsedS, (int)row.Bpm)).ToArray();
    }

    /// <summary>Which monitors recorded anything for a session.</summary>
    public async Task<IReadOnlyList<string>> ReadHeartRateSources(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            SessionMetricsStatements.SelectHeartRateSources,
            new { sessionId = SqliteValues.FromGuid(sessionId) },
            cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    /// <summary>The route recorded for a session, oldest fix first.</summary>
    public async Task<IReadOnlyList<RouteFix>> ReadRoute(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var rows = await connection.QueryAsync<RouteFixRow>(new CommandDefinition(
            SessionMetricsStatements.SelectRoute,
            new { sessionId = SqliteValues.FromGuid(sessionId) },
            cancellationToken: cancellationToken));

        return rows
            .Select(row => new RouteFix(
                (int)row.ElapsedS,
                GeoValues.FromE7(row.LatE7),
                GeoValues.FromE7(row.LonE7),
                GeoValues.FromCentimetres(row.AccuracyCm),
                (int?)row.Bpm))
            .ToArray();
    }

    private sealed record HeartRateRow(long ElapsedS, long Bpm);

    private sealed record RouteFixRow(long ElapsedS, long LatE7, long LonE7, long AccuracyCm, long? Bpm);
}
