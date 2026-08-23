using Dapper;
using Groot.Core.Contract;
using Groot.Core.Sessions;
using Microsoft.Data.Sqlite;

namespace Groot.Data.Sessions;

/// <summary>
/// Reads and writes logged sessions. A session and its sets are one aggregate: saving replaces
/// the whole set list, because a set that was removed is a set that is no longer in it. Times are
/// parameters, never <c>DateTime.Now</c>, so a test can pin them.
/// <para>
/// Every method takes the owning account. Scoping by a parameter rather than by an ambient user
/// means a caller cannot forget to scope: the signature will not let it.
/// </para>
/// </summary>
public sealed class SessionStore(GrootDatabase database)
{
    /// <summary>
    /// How many session ids go into one <c>IN</c> clause. Dapper binds each as its own parameter
    /// and SQLite caps them, so an unbounded date range would otherwise decide the query's width.
    /// </summary>
    private const int SessionIdsPerQuery = 500;

    /// <summary>
    /// Writes a session and its sets in one transaction, as an action taken on this device.
    /// Unconditional: the person holding the phone just did this, so it is by definition the
    /// newest thing that happened here.
    /// </summary>
    public Task Save(LoggedSession session, long updatedAt, string deviceId, CancellationToken cancellationToken = default) =>
        Write(SessionStatements.SaveSession, session, updatedAt, deviceId, cancellationToken);

    /// <summary>
    /// Applies a session that arrived from the server. Older than what is stored, or owned by a
    /// different account, and nothing changes — neither the session nor its sets, or a stale set
    /// list would overwrite a newer one.
    /// </summary>
    public Task Merge(LoggedSession session, long updatedAt, string deviceId, CancellationToken cancellationToken = default) =>
        Write(SessionStatements.MergeSession, session, updatedAt, deviceId, cancellationToken);

    private async Task Write(
        string statement,
        LoggedSession session,
        long updatedAt,
        string deviceId,
        CancellationToken cancellationToken)
    {
        using var connection = database.Open();
        using var transaction = connection.BeginTransaction();

        var applied = await connection.ExecuteAsync(new CommandDefinition(
            statement,
            SessionMapping.ToRow(session, updatedAt, deviceId),
            transaction,
            cancellationToken: cancellationToken));

        if (applied > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                SessionStatements.DeleteSetsOfSession,
                new { sessionId = SqliteValues.FromGuid(session.Id) },
                transaction,
                cancellationToken: cancellationToken));

            if (session.Sets.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    SessionStatements.InsertSet,
                    session.Sets.Select(SessionMapping.ToRow).ToArray(),
                    transaction,
                    cancellationToken: cancellationToken));
            }
        }

        transaction.Commit();
    }

    /// <summary>One session with its sets, or null when it is unknown, tombstoned, or not this
    /// account's. The account is a parameter so a caller holding only a route id cannot skip it.</summary>
    public async Task<LoggedSession?> Find(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(new CommandDefinition(
            SessionStatements.SelectSessionById,
            new { userId = SqliteValues.FromGuid(userId), id = SqliteValues.FromGuid(id) },
            cancellationToken: cancellationToken));

        return row is null ? null : await WithSets(connection, row, cancellationToken);
    }

    /// <summary>Every session in a date range, sets included, oldest first.</summary>
    public async Task<IReadOnlyList<LoggedSession>> Between(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var rows = (await connection.QueryAsync<SessionRow>(new CommandDefinition(
            SessionStatements.SelectSessionsBetween,
            Range(userId, from, to),
            cancellationToken: cancellationToken))).ToArray();

        if (rows.Length == 0) return [];

        var sets = await ReadSets(connection, rows.Select(r => r.Id).ToArray(), cancellationToken);
        return rows.Select(r => SessionMapping.ToDomain(r, sets.GetValueOrDefault(r.Id, []))).ToArray();
    }

    /// <summary>
    /// The week's sessions reduced to what <see cref="ContractEvaluator"/> needs. Sets are not
    /// read, because the contract counts days and kinds and nothing else.
    /// </summary>
    public async Task<IReadOnlyList<ContractSession>> ContractSessionsOfWeek(
        Guid userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var rows = await connection.QueryAsync<SessionRow>(new CommandDefinition(
            SessionStatements.SelectSessionsBetween,
            Range(userId, weekStart, weekStart.AddDays(6)),
            cancellationToken: cancellationToken));

        return rows
            .Select(r => new ContractSession(SqliteValues.ToDate(r.Date), SessionMapping.ToKind(r.Kind)))
            .ToArray();
    }

    /// <summary>Sessions per day over a range, for the history grid. Days with none are absent.</summary>
    public async Task<IReadOnlyList<DailySessionCount>> DailyCounts(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var dates = await connection.QueryAsync<string>(new CommandDefinition(
            SessionStatements.SelectSessionDates,
            Range(userId, from, to),
            cancellationToken: cancellationToken));

        return dates
            .GroupBy(SqliteValues.ToDate)
            .OrderBy(group => group.Key)
            .Select(group => new DailySessionCount(group.Key, group.Count()))
            .ToArray();
    }

    /// <summary>
    /// The most recent session of a kind within one program — what "where was I?" resolves to
    /// when a screen opens. Null when the program has not been started.
    /// </summary>
    public async Task<LoggedSession?> LatestOfKind(
        Guid userId,
        string programId,
        SessionKind kind,
        CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();

        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(new CommandDefinition(
            SessionStatements.SelectLatestOfKind,
            new
            {
                userId = SqliteValues.FromGuid(userId),
                programId,
                kind = SessionMapping.FromKind(kind),
            },
            cancellationToken: cancellationToken));

        return row is null ? null : await WithSets(connection, row, cancellationToken);
    }

    /// <summary>
    /// Tombstones a session, as an action taken on this device, and drops what it held: the note
    /// the person typed and the sets they logged. The row stays so the deletion can travel to the
    /// other device, which needs the fact of it and not the contents.
    /// </summary>
    public async Task Delete(Guid userId, Guid id, long updatedAt, string deviceId, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        using var transaction = connection.BeginTransaction();

        var applied = await connection.ExecuteAsync(new CommandDefinition(
            SessionStatements.MarkSessionDeleted,
            new { userId = SqliteValues.FromGuid(userId), id = SqliteValues.FromGuid(id), updatedAt, deviceId },
            transaction,
            cancellationToken: cancellationToken));

        // A tombstone is an UPDATE, so the cascade never fires and the sets would sit there
        // unreadable but present. Drop them here; the session row is what sync needs.
        if (applied > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                SessionStatements.DeleteSetsOfSession,
                new { sessionId = SqliteValues.FromGuid(id) },
                transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    private static object Range(Guid userId, DateOnly from, DateOnly to) => new
    {
        userId = SqliteValues.FromGuid(userId),
        from = SqliteValues.FromDate(from),
        to = SqliteValues.FromDate(to),
    };

    private static async Task<LoggedSession> WithSets(
        SqliteConnection connection,
        SessionRow row,
        CancellationToken cancellationToken)
    {
        var sets = await ReadSets(connection, [row.Id], cancellationToken);
        return SessionMapping.ToDomain(row, sets.GetValueOrDefault(row.Id, []));
    }

    private static async Task<Dictionary<string, IReadOnlyList<SetEntry>>> ReadSets(
        SqliteConnection connection,
        IReadOnlyList<string> sessionIds,
        CancellationToken cancellationToken)
    {
        var byId = new Dictionary<string, IReadOnlyList<SetEntry>>();

        for (var offset = 0; offset < sessionIds.Count; offset += SessionIdsPerQuery)
        {
            var batch = sessionIds.Skip(offset).Take(SessionIdsPerQuery).ToArray();

            var rows = await connection.QueryAsync<SetRow>(new CommandDefinition(
                SessionStatements.SelectSetsOfSessions,
                new { sessionIds = batch },
                cancellationToken: cancellationToken));

            foreach (var group in rows.GroupBy(r => r.SessionId))
                byId[group.Key] = group.Select(SessionMapping.ToDomain).ToArray();
        }

        return byId;
    }
}
