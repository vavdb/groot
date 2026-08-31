using Groot.Data;
using Groot.Data.Equipment;
using Groot.Data.Sessions;
using Groot.Data.Settings;
using Groot.Data.Users;

namespace Groot.Data.Tests;

/// <summary>
/// A real SQLite file in a temporary directory, plus the stores that read it. Tests run against
/// the actual engine rather than a mock, because what is being tested is the SQL: a mocked store
/// would pass with every column name misspelled.
/// </summary>
public sealed class TemporaryDatabase : IDisposable
{
    private readonly string _directory;

    public TemporaryDatabase()
    {
        // CreateTempSubdirectory, not a fixed /tmp/groot-tests parent: the fixed component is
        // pre-creatable by another account on a shared host, and this form is 0700 and random.
        _directory = Directory.CreateTempSubdirectory("groot-tests").FullName;

        FilePath = Path.Combine(_directory, "groot.db");
        Database = new GrootDatabase(FilePath);
        Sessions = new SessionStore(Database);
        Users = new UserStore(Database);
        Equipment = new EquipmentStore(Database);
        Settings = new SettingsStore(Database);
        Progress = new ProgramProgress(Sessions);
        Metrics = new SessionMetricsStore(Database);
    }

    public string FilePath { get; }

    public GrootDatabase Database { get; }

    public SessionStore Sessions { get; }

    public UserStore Users { get; }

    public EquipmentStore Equipment { get; }

    public SettingsStore Settings { get; }

    public ProgramProgress Progress { get; }

    public SessionMetricsStore Metrics { get; }

    /// <summary>
    /// One value straight out of the file, bypassing the stores. Some things can only be checked
    /// below the store: every read filters tombstoned rows out, so nothing above this can tell
    /// "the row is hidden" from "the row was emptied".
    /// </summary>
    public object? Scalar(string sql, Guid id)
    {
        using var connection = Database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        var value = command.ExecuteScalar();
        return value is DBNull ? null : value;
    }

    /// <summary>A second handle on the same file, as a restarted app would open it.</summary>
    public GrootDatabase Reopen() => new(FilePath);

    /// <summary>Creates an account and returns its id, since every other table needs one.</summary>
    public async Task<Guid> CreateUser(string username = "vincent")
    {
        var id = Guid.NewGuid();
        await Users.Save(new StoredUser(id, username, CreatedAt: 0));
        return id;
    }

    public void Dispose()
    {
        // SQLite holds the file until the pooled connections are closed; without this the
        // directory delete fails on Windows and leaves the temp tree behind on Linux.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temp directory is not worth failing a green test run over. Catching
            // UnauthorizedAccessException too, because letting it escape is the outcome this
            // whole block exists to prevent.
        }
    }
}
