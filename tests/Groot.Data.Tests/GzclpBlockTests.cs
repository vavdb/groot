using Groot.Core.Contract;
using Groot.Core.Equipment;
using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.Data.Tests;

/// <summary>
/// Seven GZCLP sessions in a row, through the store. Every session is checked before it is
/// logged — the day the rotation offers, and the weight and scheme of each of its three lifts —
/// so a wrong number names the session it first appeared in rather than surfacing at the end.
/// <para>
/// The sixth session misses the top set. GZCLP answers a miss by dropping a rung rather than
/// dropping weight, so the seventh has to carry that forward untouched while the lifts it does
/// train keep climbing.
/// </para>
/// </summary>
public sealed class GzclpBlockTests
{
    private const string Device = "phone";
    private static readonly DateOnly Start = new(2026, 8, 24);
    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");

    /// <summary>What goes on the bar the first time a slot is trained, before history exists.</summary>
    private static readonly Dictionary<ExerciseSlot, decimal> OpeningWeights = new()
    {
        [new("squat", 1)] = 60m,
        [new("squat", 2)] = 40m,
        [new("bench-press", 1)] = 45m,
        [new("bench-press", 2)] = 30m,
        [new("overhead-press", 1)] = 30m,
        [new("overhead-press", 2)] = 20m,
        [new("deadlift", 1)] = 70m,
        [new("deadlift", 2)] = 50m,
        [new("dumbbell-row", 3)] = 22.5m,
        [new("dumbbell-curl", 3)] = 12.5m,
        [new("dumbbell-lateral-raise", 3)] = 7.5m,
    };

    private readonly TemporaryDatabase _store = new();

    [Fact]
    public async Task The_seventh_session_is_planned_from_six_sessions_of_history()
    {
        var userId = await SixSessions();

        // A2, and none of its lifts were in the missed session: all three carry the increment
        // they earned in session 3, and none of them notice the miss.
        var seventh = await PlanFor(userId);

        Assert.Equal("A2", seventh.DayKey);
        AssertLifts(seventh,
            ("bench-press", 47.5m, "5x3+"),
            ("squat", 42.5m, "3x10"),
            ("dumbbell-curl", 12.5m, "3x15+"));
    }

    [Fact]
    public async Task The_missed_lift_holds_its_weight_and_drops_a_rung_for_when_B1_returns()
    {
        var userId = await SixSessions();

        // The rotation is four days long, so B1 is three sessions away. What matters is what it
        // will show when it arrives: the press held at 32.5 on six sets of two, and the two lifts
        // that did not miss still climbing.
        var history = await _store.Sessions.Between(userId, Start, Start.AddYears(1));
        var nextB1 = LiftSessionBuilder.For(Gzclp, "B1", LiftProgressionHistory.Replay(Gzclp, history));

        AssertLifts(nextB1,
            ("overhead-press", 32.5m, "6x2+"),
            ("deadlift", 55m, "3x10"),
            ("dumbbell-row", 22.5m, "3x15+"));
    }

    /// <summary>
    /// Six sessions through the rotation, each one checked against what the store and the replay
    /// offer before it is logged. The sixth misses its top set.
    /// </summary>
    private async Task<Guid> SixSessions()
    {
        var userId = await _store.CreateUser();
        await _store.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        // 1 — A1. Nothing has been trained, so the barbell lifts have no target yet and the
        // chin-up opens at bodyweight with nothing added.
        await Train(userId, session: 1, "A1",
            ("squat", null, "5x3+"),
            ("bench-press", null, "3x10"),
            ("chin-up", 0m, "3x15+"));

        // 2 — B1. Its three lifts are their own slots, still untrained.
        await Train(userId, session: 2, "B1",
            ("overhead-press", null, "5x3+"),
            ("deadlift", null, "3x10"),
            ("dumbbell-row", null, "3x15+"));

        // 3 — A2. Squat and bench come back at the other tier, on ladders of their own: the
        // squat trained at 60 on A1 does not hand its weight to the 3x10 squat here.
        await Train(userId, session: 3, "A2",
            ("bench-press", null, "5x3+"),
            ("squat", null, "3x10"),
            ("dumbbell-curl", null, "3x15+"));

        // 4 — B2. The deadlift's T1 increment is 5 kg where every other lift adds 2.5.
        await Train(userId, session: 4, "B2",
            ("deadlift", null, "5x3+"),
            ("overhead-press", null, "3x10"),
            ("dumbbell-lateral-raise", null, "3x15+"));

        // 5 — the rotation wraps to A1, and now every lift on it has history. The chin-up holds
        // at bodyweight: its AMRAP set stopped at fifteen, and T3 wants twenty-five.
        await Train(userId, session: 5, "A1",
            ("squat", 62.5m, "5x3+"),
            ("bench-press", 32.5m, "3x10"),
            ("chin-up", 0m, "3x15+"));

        // 6 — B1 again, and the overhead press comes up a rep short on its last set.
        await Train(userId, session: 6, "B1", missing: "overhead-press",
            ("overhead-press", 32.5m, "5x3+"),
            ("deadlift", 52.5m, "3x10"),
            ("dumbbell-row", 22.5m, "3x15+"));

        return userId;
    }

    /// <summary>
    /// The plan a screen would open on: the day the rotation offers next, loaded with where each
    /// of its lifts currently stands after replaying everything logged.
    /// </summary>
    private async Task<LiftSessionPlan> PlanFor(Guid userId)
    {
        var dayKey = await _store.Progress.NextLiftDay(userId, Gzclp);
        var history = await _store.Sessions.Between(userId, Start, Start.AddYears(1));

        return LiftSessionBuilder.For(Gzclp, dayKey, LiftProgressionHistory.Replay(Gzclp, history));
    }

    /// <summary>
    /// Checks the session the rotation offers, then logs it. Every set hits its target, except
    /// in <paramref name="missing"/>'s last set, which comes up one rep short.
    /// </summary>
    private async Task Train(
        Guid userId,
        int session,
        string expectedDay,
        params (string ExerciseId, decimal? TargetKg, string Scheme)[] expectedLifts) =>
        await Train(userId, session, expectedDay, null, expectedLifts);

    private async Task Train(
        Guid userId,
        int session,
        string expectedDay,
        string? missing,
        params (string ExerciseId, decimal? TargetKg, string Scheme)[] expectedLifts)
    {
        var plan = await PlanFor(userId);

        Assert.Equal(expectedDay, plan.DayKey);
        AssertLifts(plan, expectedLifts);

        var sessionId = Guid.NewGuid();
        var order = 0;

        var sets = plan.Exercises.SelectMany(exercise =>
        {
            var weight = exercise.TargetKg ?? OpeningWeights[new ExerciseSlot(exercise.ExerciseId, exercise.Tier)];
            var lastIndex = exercise.Sets.Count - 1;

            return exercise.Sets.Select(set => SetEntry.Total(
                sessionId,
                exercise.ExerciseId,
                order++,
                weight,
                reps: exercise.ExerciseId == missing && set.Index == lastIndex
                    ? set.TargetReps - 1
                    : set.TargetReps));
        }).ToArray();

        var date = Start.AddDays(2 * (session - 1));
        await _store.Sessions.Save(
            LoggedSession.Lift(sessionId, userId, date, Gzclp.Id, plan.DayKey, sets),
            updatedAt: date.DayNumber,
            Device);
    }

    private static void AssertLifts(
        LiftSessionPlan plan,
        params (string ExerciseId, decimal? TargetKg, string Scheme)[] expected)
    {
        Assert.Equal(
            expected.Select(e => e.ExerciseId),
            plan.Exercises.Select(e => e.ExerciseId));

        Assert.Equal(
            expected.Select(e => (e.ExerciseId, e.TargetKg, e.Scheme)),
            plan.Exercises.Select(e => (e.ExerciseId, e.TargetKg, e.Scheme.Text)));
    }
}
