using Groot.Core.Equipment;
using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.Core.Tests;

/// <summary>
/// The working weight is not stored anywhere: it is what replaying the logged sessions produces.
/// These tests are the spec for that replay.
/// </summary>
public sealed class LiftProgressionHistoryTests
{
    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 8, 24);

    [Fact]
    public void No_history_means_no_weights_rather_than_a_guess() =>
        Assert.Empty(LiftProgressionHistory.Replay(Gzclp, []));

    [Fact]
    public void The_first_session_of_a_slot_sets_where_its_ladder_starts()
    {
        var weights = WorkingWeights(Day(Monday, "A1", ("squat", 60m, 3, 5)));

        // Five clean sets of three at 60 is a good session, so the next one adds the increment.
        Assert.Equal(62.5m, weights[new ExerciseSlot("squat", 1)]);
    }

    [Fact]
    public void The_same_lift_at_two_tiers_keeps_two_ladders()
    {
        var weights = WorkingWeights(
            Day(Monday, "A1", ("squat", 60m, 3, 5)),
            Day(Monday.AddDays(2), "A2", ("squat", 40m, 10, 10)));

        Assert.Equal(62.5m, weights[new ExerciseSlot("squat", 1)]);
        Assert.Equal(42.5m, weights[new ExerciseSlot("squat", 2)]);
    }

    [Fact]
    public void Sessions_compound_across_weeks()
    {
        var weights = WorkingWeights(
            Day(Monday, "A1", ("squat", 60m, 3, 5)),
            Day(Monday.AddDays(7), "A1", ("squat", 62.5m, 3, 5)),
            Day(Monday.AddDays(14), "A1", ("squat", 65m, 3, 5)));

        Assert.Equal(67.5m, weights[new ExerciseSlot("squat", 1)]);
    }

    [Fact]
    public void A_missed_session_drops_a_rung_instead_of_adding_weight()
    {
        var state = LiftProgressionHistory.Replay(Gzclp, [Day(Monday, "A1", ("squat", 60m, 2, 5))]);

        var squat = state[new ExerciseSlot("squat", 1)];
        Assert.Equal(1, squat.Stage);
        Assert.Equal(60m, squat.WorkingWeightKg);
    }

    [Fact]
    public void What_went_on_the_bar_beats_what_was_planned()
    {
        // The lifter was due 62.5 and put 65 on instead. The next session follows the bar.
        var weights = WorkingWeights(
            Day(Monday, "A1", ("squat", 60m, 3, 5)),
            Day(Monday.AddDays(7), "A1", ("squat", 65m, 3, 5)));

        Assert.Equal(67.5m, weights[new ExerciseSlot("squat", 1)]);
    }

    [Fact]
    public void Sessions_are_replayed_oldest_first_whatever_order_they_arrive_in()
    {
        var inOrder = WorkingWeights(
            Day(Monday, "A1", ("squat", 60m, 3, 5)),
            Day(Monday.AddDays(7), "A1", ("squat", 62.5m, 3, 5)));

        var reversed = WorkingWeights(
            Day(Monday.AddDays(7), "A1", ("squat", 62.5m, 3, 5)),
            Day(Monday, "A1", ("squat", 60m, 3, 5)));

        Assert.Equal(inOrder, reversed);
    }

    [Fact]
    public void Another_programs_sessions_do_not_move_this_programs_ladders()
    {
        var foreign = Day(Monday, "A1", ("squat", 100m, 3, 5)) with { ProgramId = "some-other-program" };

        Assert.Empty(LiftProgressionHistory.Replay(Gzclp, [foreign]));
    }

    [Fact]
    public void A_T3_that_stops_at_its_target_reps_does_not_earn_the_increment()
    {
        // 3x15+ finished at exactly fifteen is 45 reps in the session and 15 on the set that
        // counts. GZCL asks for 25 on that set.
        var weights = WorkingWeights(Day(Monday, "B1", ("dumbbell-row", 22.5m, 15, 15)));

        Assert.Equal(22.5m, weights[new ExerciseSlot("dumbbell-row", 3)]);
    }

    [Fact]
    public void A_T3_that_reaches_25_on_its_last_set_climbs()
    {
        var weights = WorkingWeights(Day(Monday, "B1", ("dumbbell-row", 22.5m, 15, 25)));

        Assert.Equal(25m, weights[new ExerciseSlot("dumbbell-row", 3)]);
    }

    [Fact]
    public void A_T3_abandoned_after_one_set_does_not_progress_on_that_set()
    {
        // Three sets of fifteen, and the lifter did one set of thirty and went home. The T3 rule
        // reads the AMRAP set and never looks at completion, so treating the set they stopped at
        // as the AMRAP would add weight for a session they abandoned.
        var sessionId = Guid.NewGuid();
        var oneSet = new[] { SetEntry.Total(sessionId, "dumbbell-row", 0, 22.5m, reps: 30) };
        var session = LoggedSession.Lift(sessionId, User, Monday, Gzclp.Id, "B1", oneSet);

        Assert.Equal(22.5m, WorkingWeights(session)[new ExerciseSlot("dumbbell-row", 3)]);
    }

    [Fact]
    public void Warmups_do_not_count_as_working_sets()
    {
        var sessionId = Guid.NewGuid();
        var sets = new[]
        {
            SetEntry.Total(sessionId, "squat", 0, 100m, reps: 1) with { IsWarmup = true },
            SetEntry.Total(sessionId, "squat", 1, 60m, reps: 3),
            SetEntry.Total(sessionId, "squat", 2, 60m, reps: 3),
            SetEntry.Total(sessionId, "squat", 3, 60m, reps: 3),
            SetEntry.Total(sessionId, "squat", 4, 60m, reps: 3),
            SetEntry.Total(sessionId, "squat", 5, 60m, reps: 5),
        };
        var session = LoggedSession.Lift(sessionId, User, Monday, Gzclp.Id, "A1", sets);

        // The 100 kg single was a warmup, so the ladder is at 60 and climbs from there.
        Assert.Equal(62.5m, WorkingWeights(session)[new ExerciseSlot("squat", 1)]);
    }

    private static IReadOnlyDictionary<ExerciseSlot, decimal> WorkingWeights(params LoggedSession[] sessions) =>
        LiftProgressionHistory.Replay(Gzclp, sessions)
            .ToDictionary(entry => entry.Key, entry => entry.Value.WorkingWeightKg);

    /// <summary>
    /// One logged day: for each exercise, the weight, the reps on every set but the last, and the
    /// reps on the last. The set count comes from the tier's scheme, as it does on the screen.
    /// </summary>
    private static LoggedSession Day(
        DateOnly date,
        string dayKey,
        params (string ExerciseId, decimal WeightKg, int Reps, int LastReps)[] exercises)
    {
        var sessionId = Guid.NewGuid();
        var day = Gzclp.Day(dayKey);
        var order = 0;

        var sets = exercises.SelectMany(exercise =>
        {
            var tier = day.Exercises.First(e => e.ExerciseId == exercise.ExerciseId).Tier;
            var scheme = Gzclp.SchemeFor(tier);

            return Enumerable.Range(0, scheme.Sets).Select(index => SetEntry.Total(
                sessionId,
                exercise.ExerciseId,
                order++,
                exercise.WeightKg,
                reps: index == scheme.Sets - 1 ? exercise.LastReps : exercise.Reps));
        }).ToArray();

        return LoggedSession.Lift(sessionId, User, date, Gzclp.Id, dayKey, sets);
    }
}
