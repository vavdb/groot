using Groot.Core.Contract;
using Groot.Core.Equipment;
using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.Data.Tests;

/// <summary>
/// One week of training as it actually happens: a run on Monday, a lifting day on Tuesday, both
/// written to the store. What the next screen loads then comes from history rather than from a
/// starting table — the rotation resumes, the interval program advances, and the working weight
/// carries the result of the AMRAP set.
/// </summary>
public sealed class WeekOfTrainingTests
{
    private const string Device = "phone";
    private static readonly DateOnly Monday = new(2026, 8, 24);
    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");
    private static readonly IntervalProgram Couch = ProgramCatalog.Embedded.IntervalProgram("0-to-5k");

    [Fact]
    public async Task An_empty_history_starts_both_programs_at_their_first_session()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        Assert.Equal("A1", await temp.Progress.NextLiftDay(userId, Gzclp));
        Assert.Equal(new IntervalSession(1, 1), await temp.Progress.NextRun(userId, Couch));
    }

    [Fact]
    public async Task The_run_after_week_one_day_one_is_week_one_day_two()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Sessions.Save(
            LoggedSession.Run(Guid.NewGuid(), userId, Monday, Couch.Id, week: 1, day: 1, durationSeconds: 1800),
            updatedAt: 100,
            Device);

        Assert.Equal(new IntervalSession(1, 2), await temp.Progress.NextRun(userId, Couch));
    }

    [Fact]
    public async Task The_last_run_of_a_week_rolls_over_into_the_next_week()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var lastOfWeekOne = Couch.Week(1).DayNumbers[^1];

        await temp.Sessions.Save(
            LoggedSession.Run(Guid.NewGuid(), userId, Monday, Couch.Id, week: 1, day: lastOfWeekOne),
            updatedAt: 100,
            Device);

        Assert.Equal(new IntervalSession(2, 1), await temp.Progress.NextRun(userId, Couch));
    }

    [Fact]
    public async Task The_gzclp_day_after_A1_is_B1()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        await LogLiftDay(temp, userId, Monday.AddDays(1), "A1", squatKg: 60m, amrapReps: 5);

        Assert.Equal("B1", await temp.Progress.NextLiftDay(userId, Gzclp));
    }

    [Fact]
    public async Task A_lifting_day_the_program_no_longer_rotates_through_restarts_the_rotation()
    {
        // A program's rotation can change between versions while its history stays. Resuming from
        // a day that no longer exists has to open on A1, not throw on the way to the screen.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Sessions.Save(
            LoggedSession.Lift(Guid.NewGuid(), userId, Monday, Gzclp.Id, "C3", []), updatedAt: 100, Device);

        Assert.Equal("A1", await temp.Progress.NextLiftDay(userId, Gzclp));
    }

    [Fact]
    public async Task A_run_the_program_no_longer_has_restarts_the_program()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Sessions.Save(
            LoggedSession.Run(Guid.NewGuid(), userId, Monday, Couch.Id, week: 1, day: 99),
            updatedAt: 100,
            Device);

        Assert.Equal(new IntervalSession(1, 1), await temp.Progress.NextRun(userId, Couch));
    }

    [Fact]
    public async Task A_run_and_a_lift_in_the_same_week_advance_their_own_programs_independently()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        await temp.Sessions.Save(
            LoggedSession.Run(Guid.NewGuid(), userId, Monday, Couch.Id, week: 1, day: 1),
            updatedAt: 100,
            Device);
        await LogLiftDay(temp, userId, Monday.AddDays(1), "A1", squatKg: 60m, amrapReps: 5);

        Assert.Equal(new IntervalSession(1, 2), await temp.Progress.NextRun(userId, Couch));
        Assert.Equal("B1", await temp.Progress.NextLiftDay(userId, Gzclp));

        // Both sessions land in the same contract week, one day apart.
        var week = await temp.Sessions.ContractSessionsOfWeek(userId, Monday);
        Assert.Equal(2, week.Count);
        Assert.Equal([SessionKind.Run, SessionKind.Lift], week.Select(s => s.Kind));
    }

    [Fact]
    public async Task A_clean_A1_raises_the_squat_for_the_session_that_follows_it()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        await LogLiftDay(temp, userId, Monday.AddDays(1), "A1", squatKg: 60m, amrapReps: 5);

        var nextA1 = LiftSessionBuilder.For(Gzclp, "A1", await Replay(temp, userId));
        Assert.Equal(62.5m, nextA1.Exercises.Single(e => e.ExerciseId == "squat").TargetKg);
    }

    [Fact]
    public async Task The_T2_squat_keeps_its_own_weight_when_the_T1_squat_climbs()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        // A1 trains squat as T1 at 60; A2 trains it as T2 at 40. They are separate ladders.
        await LogLiftDay(temp, userId, Monday.AddDays(1), "A1", squatKg: 60m, amrapReps: 5);
        await LogLiftDay(temp, userId, Monday.AddDays(3), "A2", squatKg: 40m, amrapReps: 10);

        var state = await Replay(temp, userId);

        Assert.Equal(62.5m, state[new ExerciseSlot("squat", 1)].WorkingWeightKg);
        Assert.Equal(42.5m, state[new ExerciseSlot("squat", 2)].WorkingWeightKg);
    }

    /// <summary>
    /// Logs one rotation day as a completed session: the only write a lifting screen performs,
    /// now that the working weight is replayed rather than stored.
    /// </summary>
    private static async Task LogLiftDay(
        TemporaryDatabase temp,
        Guid userId,
        DateOnly date,
        string dayKey,
        decimal squatKg,
        int amrapReps)
    {
        var sessionId = Guid.NewGuid();
        var day = Gzclp.Day(dayKey);
        var topExercise = day.Exercises.First(e => e.ExerciseId == "squat");
        var scheme = Gzclp.SchemeFor(topExercise.Tier);
        var bar = EquipmentProfile.Rack.Bar;
        var sideKg = (squatKg - bar.EffectiveBarKg) / 2m;

        var sets = Enumerable.Range(0, scheme.Sets)
            .Select(index => SetEntry.PerSide(
                sessionId, topExercise.ExerciseId, index, bar, sideKg,
                reps: index == scheme.Sets - 1 ? amrapReps : scheme.Reps))
            .ToArray();

        await temp.Sessions.Save(
            LoggedSession.Lift(sessionId, userId, date, Gzclp.Id, dayKey, sets),
            updatedAt: date.DayNumber,
            Device);

    }

    /// <summary>Where the lifts stand, as a screen resolves it: replayed from the stored sessions.</summary>
    private static async Task<IReadOnlyDictionary<ExerciseSlot, ExerciseState>> Replay(
        TemporaryDatabase temp,
        Guid userId)
    {
        var history = await temp.Sessions.Between(userId, Monday, Monday.AddDays(30));
        return LiftProgressionHistory.Replay(Gzclp, history);
    }
}
