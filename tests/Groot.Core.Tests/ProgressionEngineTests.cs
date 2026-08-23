using Groot.Core.Programs;

namespace Groot.Core.Tests;

public class ProgressionEngineTests
{
    // GZCLP T1: linear +2.5 on success, fail-ladder 5x3 -> 6x2 -> 10x1 -> reset 90%
    private static readonly IProgressionRule[] T1Rules =
    [
        new LinearIncrement(2.5m),
        new FailLadder(Stages: 2, ResetPct: 0.9m),
    ];

    [Fact]
    public void Success_adds_increment()
    {
        var state = new ExerciseState(60m, Stage: 0, LastBaseWeightKg: 60m);
        var decision = ProgressionEngine.Advance(T1Rules, state, new SessionResult(true, 16));

        Assert.Equal(62.5m, decision.NextWeightKg);
        Assert.Equal(0, decision.NextStage);
    }

    [Fact]
    public void Failure_moves_down_ladder_at_same_weight()
    {
        var state = new ExerciseState(100m, Stage: 0, LastBaseWeightKg: 100m);
        var decision = ProgressionEngine.Advance(T1Rules, state, new SessionResult(false, 12));

        Assert.Equal(100m, decision.NextWeightKg);
        Assert.Equal(1, decision.NextStage);
    }

    [Fact]
    public void Failure_past_last_stage_resets_to_90_percent()
    {
        var state = new ExerciseState(100m, Stage: 2, LastBaseWeightKg: 100m);
        var decision = ProgressionEngine.Advance(T1Rules, state, new SessionResult(false, 8));

        Assert.Equal(90m, decision.NextWeightKg);
        Assert.Equal(0, decision.NextStage);
    }

    [Fact]
    public void T3_progresses_when_the_amrap_set_reaches_25_reps()
    {
        var rules = new IProgressionRule[] { new AmrapSetThreshold(25, 2.5m) };
        var state = new ExerciseState(40m, 0, 40m);

        var below = ProgressionEngine.Advance(rules, state, new SessionResult(true, TotalReps: 54, AmrapReps: 24));
        var at = ProgressionEngine.Advance(rules, state, new SessionResult(true, TotalReps: 55, AmrapReps: 25));

        Assert.Equal(40m, below.NextWeightKg);
        Assert.Equal(42.5m, at.NextWeightKg);
    }

    [Fact]
    public void A_high_session_total_does_not_stand_in_for_the_amrap_set()
    {
        // 3x15+ finished at exactly fifteen is 45 reps. Reading the session total would clear a
        // 25-rep threshold every time and add weight to a lift that never earned it.
        var rules = new IProgressionRule[] { new AmrapSetThreshold(25, 2.5m) };
        var state = new ExerciseState(40m, 0, 40m);

        var decision = ProgressionEngine.Advance(rules, state, new SessionResult(true, TotalReps: 45, AmrapReps: 15));

        Assert.Equal(40m, decision.NextWeightKg);
    }

    [Fact]
    public void A_scheme_with_no_amrap_set_cannot_clear_an_amrap_threshold()
    {
        var rules = new IProgressionRule[] { new AmrapSetThreshold(25, 2.5m) };
        var state = new ExerciseState(40m, 0, 40m);

        var decision = ProgressionEngine.Advance(rules, state, new SessionResult(true, TotalReps: 30, AmrapReps: null));

        Assert.Equal(40m, decision.NextWeightKg);
    }
}
