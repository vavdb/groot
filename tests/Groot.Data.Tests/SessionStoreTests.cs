using Groot.Core.Contract;
using Groot.Core.Equipment;
using Groot.Core.Sessions;

namespace Groot.Data.Tests;

/// <summary>
/// Round-trips through a real SQLite file. Records give value equality, so one assert per session
/// covers every column: a mistyped column name or a mismatched enum fails here immediately.
/// </summary>
public sealed class SessionStoreTests
{
    private const string Device = "phone";
    private static readonly DateOnly Monday = new(2026, 8, 24);

    [Fact]
    public async Task A_lift_session_and_its_sets_come_back_exactly_as_they_were_saved()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);
        await temp.Equipment.Save(userId, new EquipmentProfile(DumbbellInPounds, []), updatedAt: 1, Device);

        var sessionId = Guid.NewGuid();
        var bar = EquipmentProfile.Rack.Bar;
        var sets = new[]
        {
            SetEntry.PerSide(sessionId, "squat", 0, bar, sideKg: 30m, reps: 3),
            SetEntry.PerSide(sessionId, "squat", 1, bar, sideKg: 30m, reps: 3),
            SetEntry.Total(sessionId, "bench", 2, totalKg: 45m, reps: 10),
            SetEntry.PerHand(sessionId, "db-row", 3, DumbbellInPounds, perHand: 40m, reps: 15),
        };
        var session = LoggedSession.Lift(sessionId, userId, Monday, "gzclp-rack", "A1", sets, durationSeconds: 3120);

        await temp.Sessions.Save(session, updatedAt: 10, Device);
        var read = await temp.Sessions.Find(userId, sessionId);

        SessionAssert.Matches(session, read);
    }

    [Fact]
    public async Task A_run_session_comes_back_exactly_as_it_was_saved()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        var sessionId = Guid.NewGuid();
        var session = LoggedSession.Run(
            sessionId, userId, Monday.AddDays(1), "0-to-5k", week: 1, day: 1, durationSeconds: 1800);

        await temp.Sessions.Save(session, updatedAt: 10, Device);
        var read = await temp.Sessions.Find(userId, sessionId);

        SessionAssert.Matches(session, read);
        Assert.Empty(read!.Sets);
    }

    [Fact]
    public async Task A_session_survives_closing_and_reopening_the_database()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();
        var session = LoggedSession.Run(sessionId, userId, Monday, "0-to-5k", week: 1, day: 1);

        await temp.Sessions.Save(session, updatedAt: 10, Device);

        var afterRestart = new Groot.Data.Sessions.SessionStore(temp.Reopen());
        SessionAssert.Matches(session, await afterRestart.Find(userId, sessionId));
    }

    [Fact]
    public async Task Saving_a_session_again_replaces_its_set_list_rather_than_adding_to_it()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();

        var three = Enumerable.Range(0, 3)
            .Select(index => SetEntry.Total(sessionId, "squat", index, 60m, reps: 3))
            .ToArray();
        await temp.Sessions.Save(
            LoggedSession.Lift(sessionId, userId, Monday, "gzclp-rack", "A1", three), updatedAt: 10, Device);

        var two = three.Take(2).ToArray();
        await temp.Sessions.Save(
            LoggedSession.Lift(sessionId, userId, Monday, "gzclp-rack", "A1", two), updatedAt: 20, Device);

        var read = await temp.Sessions.Find(userId, sessionId);
        Assert.Equal(2, read!.Sets.Count);
    }

    [Fact]
    public async Task A_session_arriving_older_than_the_stored_one_is_ignored_along_with_its_sets()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();

        var newer = LoggedSession.Lift(
            sessionId, userId, Monday, "gzclp-rack", "A1",
            [SetEntry.Total(sessionId, "squat", 0, 60m, reps: 3)],
            notes: "the one that stuck");
        await temp.Sessions.Save(newer, updatedAt: 200, Device);

        var older = newer with { Notes = "the stale one", Sets = [] };
        await temp.Sessions.Merge(older, updatedAt: 100, "tablet");

        SessionAssert.Matches(newer, await temp.Sessions.Find(userId, sessionId));
    }

    [Fact]
    public async Task Two_sessions_arriving_with_the_same_timestamp_resolve_the_same_way_every_time()
    {
        // Without a tie-break each device keeps its own version and the two never converge.
        // Which one wins is arbitrary; that both sides pick the same one is the point.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();

        var fromAlpha = LoggedSession.Run(sessionId, userId, Monday, "0-to-5k", 1, 1, notes: "alpha");
        var fromBeta = fromAlpha with { Notes = "beta" };

        await temp.Sessions.Merge(fromAlpha, updatedAt: 100, "alpha");
        await temp.Sessions.Merge(fromBeta, updatedAt: 100, "beta");

        Assert.Equal("beta", (await temp.Sessions.Find(userId, sessionId))!.Notes);
    }

    [Fact]
    public async Task A_local_save_wins_even_when_it_lands_in_the_same_millisecond_as_the_last_one()
    {
        // The user tapped twice. Applying the merge rule to local writes drops the second edit,
        // which is why saving and merging are separate operations.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();

        var first = LoggedSession.Run(sessionId, userId, Monday, "0-to-5k", 1, 1, notes: "first tap");
        await temp.Sessions.Save(first, updatedAt: 100, Device);
        await temp.Sessions.Save(first with { Notes = "second tap" }, updatedAt: 100, Device);

        Assert.Equal("second tap", (await temp.Sessions.Find(userId, sessionId))!.Notes);
    }

    [Fact]
    public async Task A_deleted_session_stops_being_readable_but_keeps_its_row_for_sync()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();
        await temp.Sessions.Save(
            LoggedSession.Run(sessionId, userId, Monday, "0-to-5k", 1, 1), updatedAt: 10, Device);

        await temp.Sessions.Delete(userId, sessionId, updatedAt: 20, Device);

        Assert.Null(await temp.Sessions.Find(userId, sessionId));
        Assert.Empty(await temp.Sessions.ContractSessionsOfWeek(userId, Monday));
    }

    [Fact]
    public async Task One_account_cannot_read_or_delete_another_accounts_session()
    {
        // There is one user today. These two signatures are what a request handler will call once
        // there is a server, and a handler holding only a route id cannot scope a query the
        // method never asked it to scope.
        using var temp = new TemporaryDatabase();
        var alice = await temp.CreateUser("alice");
        var bob = await temp.CreateUser("bob");
        var sessionId = Guid.NewGuid();

        await temp.Sessions.Save(
            LoggedSession.Run(sessionId, alice, Monday, "0-to-5k", 1, 1, notes: "alice ran"),
            updatedAt: 10,
            Device);

        Assert.Null(await temp.Sessions.Find(bob, sessionId));

        await temp.Sessions.Delete(bob, sessionId, updatedAt: 20, Device);
        Assert.NotNull(await temp.Sessions.Find(alice, sessionId));
    }

    [Fact]
    public async Task A_session_arriving_under_another_accounts_ownership_is_refused()
    {
        using var temp = new TemporaryDatabase();
        var alice = await temp.CreateUser("alice");
        var mallory = await temp.CreateUser("mallory");
        var sessionId = Guid.NewGuid();

        var alices = LoggedSession.Run(sessionId, alice, Monday, "0-to-5k", 1, 1, notes: "alice ran");
        await temp.Sessions.Save(alices, updatedAt: 10, Device);

        // Same session id, a different owner, a newer timestamp. Without the ownership guard the
        // merge rewrites Alice's row in place and she reads the result back as her own.
        var forged = alices with { UserId = mallory, Notes = "not alice" };
        await temp.Sessions.Merge(forged, updatedAt: 999, "attacker");

        SessionAssert.Matches(alices, await temp.Sessions.Find(alice, sessionId));
    }

    [Fact]
    public async Task A_deleted_session_keeps_no_note_and_no_sets()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();
        await temp.Sessions.Save(
            LoggedSession.Lift(sessionId, userId, Monday, "gzclp-rack", "A1",
                [SetEntry.Total(sessionId, "squat", 0, 60m, reps: 3)], notes: "felt heavy"),
            updatedAt: 10,
            Device);

        await temp.Sessions.Delete(userId, sessionId, updatedAt: 20, Device);

        // Read the file rather than the store: every store query filters deleted = 0, so it can
        // only show that the row is hidden, not that the note and the sets are actually gone.
        Assert.Null(temp.Scalar("SELECT notes FROM sessions WHERE id = $id", sessionId));
        Assert.Equal(0L, temp.Scalar("SELECT COUNT(*) FROM sets WHERE session_id = $id", sessionId));
        Assert.Equal(1L, temp.Scalar("SELECT deleted FROM sessions WHERE id = $id", sessionId));
    }

    [Fact]
    public async Task The_week_reduces_to_the_dates_and_kinds_the_contract_counts()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await Save(temp, userId, LoggedSession.Run(Guid.NewGuid(), userId, Monday, "0-to-5k", 1, 1));
        await Save(temp, userId, LoggedSession.Lift(
            Guid.NewGuid(), userId, Monday.AddDays(1), "gzclp-rack", "A1", []));
        // Next Monday: outside the window, and the proof that the range is inclusive-exclusive
        // in the right places.
        await Save(temp, userId, LoggedSession.Run(Guid.NewGuid(), userId, Monday.AddDays(7), "0-to-5k", 1, 2));

        var week = await temp.Sessions.ContractSessionsOfWeek(userId, Monday);

        Assert.Equal(
            [new ContractSession(Monday, SessionKind.Run), new ContractSession(Monday.AddDays(1), SessionKind.Lift)],
            week);
    }

    [Fact]
    public async Task Daily_counts_over_an_empty_history_come_back_empty_rather_than_throwing()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        Assert.Empty(await temp.Sessions.DailyCounts(userId, Monday, Monday.AddDays(6)));
    }

    [Fact]
    public async Task Daily_counts_group_by_day_and_skip_days_with_nothing_on_them()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await Save(temp, userId, LoggedSession.Run(Guid.NewGuid(), userId, Monday, "0-to-5k", 1, 1));
        await Save(temp, userId, LoggedSession.Lift(Guid.NewGuid(), userId, Monday, "gzclp-rack", "A1", []));
        await Save(temp, userId, LoggedSession.Run(Guid.NewGuid(), userId, Monday.AddDays(2), "0-to-5k", 1, 2));

        var counts = await temp.Sessions.DailyCounts(userId, Monday, Monday.AddDays(6));

        Assert.Equal(
            [new DailySessionCount(Monday, 2), new DailySessionCount(Monday.AddDays(2), 1)],
            counts);
    }

    private static Core.Equipment.Equipment DumbbellInPounds =>
        new("powerblock", "PowerBlock", EquipmentKind.AdjustableDumbbell, WeightUnit.Lb);

    private static Task Save(TemporaryDatabase temp, Guid userId, LoggedSession session) =>
        temp.Sessions.Save(session, updatedAt: session.Date.DayNumber, Device);
}
