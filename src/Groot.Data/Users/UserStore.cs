using Dapper;

namespace Groot.Data.Users;

/// <summary>An account on this device. No personal data: a username and when it was created.</summary>
public sealed record StoredUser(Guid Id, string Username, long CreatedAt);

/// <summary>Reads and writes the account rows every other table hangs off.</summary>
public sealed class UserStore(GrootDatabase database)
{
    private const string Insert = """
        INSERT INTO users (id, username, created_at) VALUES (@Id, @Username, @CreatedAt)
        ON CONFLICT(id) DO UPDATE SET username = excluded.username
        """;

    private const string SelectByUsername = "SELECT id, username, created_at FROM users WHERE username = @username";

    /// <summary>Creates the account, or renames an existing one with the same id.</summary>
    public async Task Save(StoredUser user, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        await connection.ExecuteAsync(new CommandDefinition(Insert, new
        {
            Id = SqliteValues.FromGuid(user.Id),
            user.Username,
            user.CreatedAt,
        }, cancellationToken: cancellationToken));
    }

    /// <summary>The account with this username, or null.</summary>
    public async Task<StoredUser?> FindByUsername(string username, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(SelectByUsername, new { username }, cancellationToken: cancellationToken));
        return row is null ? null : new StoredUser(SqliteValues.ToGuid(row.Id), row.Username, row.CreatedAt);
    }

    private sealed record UserRow(string Id, string Username, long CreatedAt);
}
