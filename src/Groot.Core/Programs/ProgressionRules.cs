namespace Groot.Core.Programs;

/// <summary>
/// Result of one exercise in one session, as progression rules see it. <paramref name="AmrapReps"/>
/// is the last set's reps when the scheme ends in one, and null otherwise — a straight 3x10 has
/// no AMRAP set to read.
/// </summary>
public sealed record SessionResult(bool AllSetsCompleted, int TotalReps, int? AmrapReps = null);

/// <summary>Rolling progression state for one exercise inside a program instance.</summary>
public sealed record ExerciseState(decimal WorkingWeightKg, int Stage, decimal LastBaseWeightKg)
{
    /// <summary>A lift not yet trained: the weight it opens at, on the tier's own scheme.</summary>
    public static ExerciseState Starting(decimal weightKg) => new(weightKg, Stage: 0, weightKg);
}

public sealed record ProgressionDecision(decimal NextWeightKg, int NextStage, string Explanation);

/// <summary>
/// Composition over inheritance: a program exercise owns an ordered list of rules;
/// the first rule that applies decides. New programs are new data, not new classes.
/// </summary>
public interface IProgressionRule
{
    ProgressionDecision? Evaluate(ExerciseState state, SessionResult result);
}

/// <summary>On success: add the increment, stay at stage 0's scheme.</summary>
public sealed record LinearIncrement(decimal Kg) : IProgressionRule
{
    public ProgressionDecision? Evaluate(ExerciseState state, SessionResult result) =>
        result.AllSetsCompleted
            ? new(state.WorkingWeightKg + Kg, state.Stage, $"+{Kg} kg")
            : null;
}

/// <summary>
/// On failure: drop down the ladder (5x3 -> 6x2 -> 10x1) at the same weight;
/// past the last stage, reset to a percentage of the last base weight.
/// </summary>
public sealed record FailLadder(int Stages, decimal ResetPct) : IProgressionRule
{
    public ProgressionDecision? Evaluate(ExerciseState state, SessionResult result)
    {
        if (result.AllSetsCompleted) return null;
        return state.Stage < Stages
            ? new(state.WorkingWeightKg, state.Stage + 1, "same weight, next stage")
            : new(Math.Round(state.LastBaseWeightKg * ResetPct, 1), 0, $"reset to {ResetPct:P0}");
    }
}

/// <summary>
/// On failure past the last rung: keep the ladder's shape but add weight and start over, which is
/// how GZCLP restarts a T2 rather than dropping it back down.
/// </summary>
public sealed record FailLadderBump(int Stages, decimal BumpKg) : IProgressionRule
{
    public ProgressionDecision? Evaluate(ExerciseState state, SessionResult result)
    {
        if (result.AllSetsCompleted) return null;
        return state.Stage < Stages
            ? new(state.WorkingWeightKg, state.Stage + 1, "same weight, next stage")
            : new(state.WorkingWeightKg + BumpKg, 0, $"+{BumpKg} kg, scheme restarts");
    }
}

/// <summary>
/// T3-style: progress when the AMRAP set alone reaches the threshold. GZCL's rule is 25 or more
/// reps on the last set, not across the session — a 3x15+ finished at exactly fifteen adds up to
/// 45 and would clear any session total worth setting, so counting the session would add weight
/// every time.
/// </summary>
public sealed record AmrapSetThreshold(int Reps, decimal Kg) : IProgressionRule
{
    public ProgressionDecision? Evaluate(ExerciseState state, SessionResult result) =>
        result.AmrapReps >= Reps
            ? new(state.WorkingWeightKg + Kg, state.Stage, $"{result.AmrapReps} on the last set, +{Kg} kg")
            : new(state.WorkingWeightKg, state.Stage, "below threshold, same weight");
}

public static class ProgressionEngine
{
    /// <summary>First rule that returns a decision wins; no rule deciding means no change.</summary>
    public static ProgressionDecision Advance(
        IReadOnlyList<IProgressionRule> rules, ExerciseState state, SessionResult result) =>
        rules.Select(r => r.Evaluate(state, result)).FirstOrDefault(d => d is not null)
        ?? new ProgressionDecision(state.WorkingWeightKg, state.Stage, "no change");
}
