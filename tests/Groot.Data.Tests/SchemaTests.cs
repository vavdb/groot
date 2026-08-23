namespace Groot.Data.Tests;

/// <summary>The schema is applied from schema.v1.sql and versioned, so an older file can catch up.</summary>
public sealed class SchemaTests
{
    [Fact]
    public void Opening_an_empty_file_applies_the_schema_and_records_its_version()
    {
        using var temp = new TemporaryDatabase();

        Assert.Equal(0, temp.Database.ReadSchemaVersion());

        using (temp.Database.Open())
        {
            // Opening is what applies the schema; nothing else to do here.
        }

        Assert.Equal(GrootDatabase.LatestSchemaVersion, temp.Database.ReadSchemaVersion());
    }

    [Fact]
    public async Task Reopening_an_existing_database_keeps_its_data_and_its_version()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        var reopened = new Groot.Data.Users.UserStore(temp.Reopen());
        var user = await reopened.FindByUsername("vincent");

        Assert.Equal(GrootDatabase.LatestSchemaVersion, temp.Database.ReadSchemaVersion());
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
    }

    [Fact]
    public async Task A_lifting_session_without_a_rotation_day_is_refused_by_the_schema()
    {
        // Stored, it would render and credit the week contract, then fall out of progression
        // without a sound, because LiftProgressionHistory has no day to read tiers from.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        var dayless = Groot.Core.Sessions.LoggedSession.Lift(
            Guid.NewGuid(), userId, new DateOnly(2026, 8, 24), "gzclp-rack", dayKey: "", []) with { DayKey = null };

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => temp.Sessions.Save(dayless, updatedAt: 1, deviceId: "test"));
    }

    [Fact]
    public async Task A_run_without_a_place_in_its_program_is_refused_by_the_schema()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        var placeless = Groot.Core.Sessions.LoggedSession.Run(
            Guid.NewGuid(), userId, new DateOnly(2026, 8, 24), "0-to-5k", 1, 1) with { IntervalDay = null };

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => temp.Sessions.Save(placeless, updatedAt: 1, deviceId: "test"));
    }

    [Fact]
    public async Task Foreign_keys_are_enforced_so_an_orphan_session_cannot_be_written()
    {
        using var temp = new TemporaryDatabase();

        var orphan = Groot.Core.Sessions.LoggedSession.RestClaim(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 24));

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => temp.Sessions.Save(orphan, updatedAt: 1, deviceId: "test"));
    }
}
