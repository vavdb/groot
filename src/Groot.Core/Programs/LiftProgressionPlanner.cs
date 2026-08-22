namespace Groot.Core.Programs;

/// <summary>
/// What one exercise did in one session: the weight it was trained at, the rung of the fail
/// ladder it was on, whether every set hit its target, and the reps that were actually logged.
/// </summary>
public sealed record ExerciseOutcome(
    string ExerciseId,
    int Tier,
    decimal WeightKg,
    int Stage,
    bool AllSetsCompleted,
    int TotalReps);

/// <summary>Where the next session starts: the weight, the scheme, the rung, and why.</summary>
public sealed record NextSession(decimal WeightKg, SetScheme Scheme, int Stage, string Explanation);

/// <summary>
/// Reads a session and says what the next one looks like. It owns no rules of its own: it builds
/// the tier's rules from the program definition and hands them to <see cref="ProgressionEngine"/>,
/// so a program changes its progression by changing its JSON.
/// </summary>
public static class LiftProgressionPlanner
{
    public static NextSession Next(LiftProgram program, ExerciseOutcome outcome)
    {
        var tier = program.TierFor(outcome.Tier);
        var state = new ExerciseState(outcome.WeightKg, outcome.Stage, LastBaseWeightKg: outcome.WeightKg);
        var result = new SessionResult(outcome.AllSetsCompleted, outcome.TotalReps);

        var decision = ProgressionEngine.Advance(RulesFor(tier, outcome.ExerciseId), state, result);

        return new NextSession(decision.NextWeightKg, tier.SchemeAt(decision.NextStage), decision.NextStage, decision.Explanation);
    }

    /// <summary>
    /// Tiers that climb on total reps (T3) use the threshold; tiers with a ladder (T1, T2) add
    /// weight on a clean session and drop a rung on a failure. How the ladder ends is the tier's
    /// own business: a percentage reset, or a bump and a restart.
    /// </summary>
    private static IReadOnlyList<IProgressionRule> RulesFor(TierProgression tier, string exerciseId)
    {
        var increment = tier.IncrementFor(exerciseId);

        if (tier.ProgressAtTotalReps is { } threshold)
            return [new AmrapThreshold(threshold, increment)];

        var rules = new List<IProgressionRule> { new LinearIncrement(increment) };

        if (tier.ResetPctOfLast is { } pct)
            rules.Add(new FailLadder(tier.FailLadder.Count, pct));
        else if (tier.ResetBumpKg is { } bump)
            rules.Add(new FailLadderBump(tier.FailLadder.Count, bump));
        else if (tier.FailLadder.Count > 0)
            rules.Add(new FailLadder(tier.FailLadder.Count, ResetPct: 1m));

        return rules;
    }
}
