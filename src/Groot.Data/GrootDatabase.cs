using System.Reflection;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Groot.Data;

/// <summary>
/// One SQLite file, and the only thing in the app that knows how to open it. Every connection
/// comes back with foreign keys enforced; the first one applies any schema versions the file is
/// behind on, so callers never have to ask whether the database has been set up.
/// </summary>
public sealed class GrootDatabase
{
    /// <summary>Highest schema version this build knows. One embedded script per version.</summary>
    public const int LatestSchemaVersion = 1;

    /// <summary>
    /// How long a write waits for the one writer WAL allows before giving up. Stated rather than
    /// inherited: without it a settings save landing during a session save fails immediately
    /// instead of waiting the moment or two the other write actually needs.
    /// </summary>
    private static readonly TimeSpan BusyTimeout = TimeSpan.FromSeconds(5);

    static GrootDatabase()
    {
        // Lets snake_case columns bind to PascalCase row properties, so session_id reaches
        // SessionId without an alias in every SELECT. Without it Dapper leaves the property at
        // its default and the mismatch surfaces as an empty GUID rather than as an error. Every
        // store reaches the database through this class, so this runs before any query does.
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private readonly string _connectionString;
    private readonly Lock _migrationGate = new();
    // Read outside the lock in the fast path, so the write has to be published.
    private volatile bool _migrated;

    /// <summary>Opens (and creates, if missing) the database at <paramref name="filePath"/>.</summary>
    public GrootDatabase(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // Microsoft.Data.Sqlite passes a DataSource beginning with "file:" through as a SQLite
        // URI, where ?mode=memory turns the store into one that accepts every write and keeps
        // nothing. Reject the form outright rather than trusting every future caller to.
        if (filePath.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The database path must be a file path, not a SQLite URI.", nameof(filePath));

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(filePath),
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            DefaultTimeout = (int)BusyTimeout.TotalSeconds,
        }.ToString();
    }

    /// <summary>
    /// An open connection with the schema already up to date. The caller disposes it.
    /// <para>
    /// Synchronous on purpose: opening a local SQLite file is a handful of microseconds, and an
    /// async wrapper would buy a state machine per query and nothing else. The one call that is
    /// not cheap is the first, which applies the schema, so a head should make it during startup
    /// rather than let it land inside whichever query happens to run first on the UI thread.
    /// </para>
    /// </summary>
    public SqliteConnection Open()
    {
        EnsureSchema();

        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>The schema version recorded in the file. Zero means an empty database.</summary>
    public int ReadSchemaVersion()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return ReadSchemaVersion(connection);
    }

    private void EnsureSchema()
    {
        if (_migrated) return;

        lock (_migrationGate)
        {
            if (_migrated) return;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // WAL survives the connection and only has to be asked for once, but asking again is
            // free and keeps a database created by an older build from staying in rollback mode.
            Execute(connection, "PRAGMA journal_mode = WAL;");

            var current = ReadSchemaVersion(connection);
            for (var version = current + 1; version <= LatestSchemaVersion; version++)
            {
                using var transaction = connection.BeginTransaction();

                // Re-read inside the write lock. Two processes opening a fresh file both see 0
                // outside it; the loser would otherwise wake up and try to create users twice.
                if (ReadSchemaVersion(connection) >= version)
                {
                    transaction.Rollback();
                    continue;
                }

                // A real v2 is SQLite's twelve-step table rebuild, which needs foreign keys out of
                // the way. PRAGMA foreign_keys is a no-op inside a transaction; defer_foreign_keys
                // is not, so this is the form that will actually work when v2 arrives.
                Execute(connection, "PRAGMA defer_foreign_keys = ON;", transaction);
                Execute(connection, ReadScript(version), transaction);
                // user_version takes no parameter, and the value is an int this class produced.
                Execute(connection, $"PRAGMA user_version = {version};", transaction);
                transaction.Commit();
            }

            _migrated = true;
        }
    }

    private static int ReadSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.ExecuteNonQuery();
    }

    private static string ReadScript(int version)
    {
        var name = $"Groot.Data.Schema.schema.v{version}.sql";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Schema script '{name}' is not embedded in Groot.Data.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
