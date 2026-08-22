using Groot.Core.Programs;

namespace Groot.Core.Sessions;

/// <summary>One planned set: its place in the exercise, the reps to hit, and whether it is the AMRAP.</summary>
public sealed record LiftSet(int Index, int TargetReps, bool IsAmrap);

/// <summary>
/// One exercise as it is trained today. What <see cref="TargetKg"/> means follows the loading: a
/// total on the bar, the weight in each hand, or the adjustment to bodyweight, which is negative
/// when the lift is assisted.
/// <para>
/// The per-hand reading matters when logged sets start feeding next session's targets:
/// <see cref="SetEntry.PerHand"/> stores <c>WeightKg</c> as both hands, so a store must halve it
/// before it comes back here, or the dumbbell weight doubles every session.
/// </para>
/// </summary>
public sealed record LiftExercisePlan(
    string ExerciseId,
    int Tier,
    LoadingKind Loading,
    SetScheme Scheme,
    decimal? TargetKg,
    int RestSeconds,
    IReadOnlyList<LiftSet> Sets)
{
    /// <summary>Reps if every set lands exactly on target. The AMRAP set can only add to this.</summary>
    public int PlannedReps => Sets.Sum(s => s.TargetReps);
}

/// <summary>A training day, resolved: the program, which day of the rotation, and every set in it.</summary>
public sealed record LiftSessionPlan(
    string ProgramId,
    string ProgramName,
    string DayKey,
    IReadOnlyList<LiftExercisePlan> Exercises)
{
    public int TotalSets => Exercises.Sum(e => e.Sets.Count);
}

/// <summary>
/// Turns a program day plus the lifter's working weights into the sets to perform. Pure: it reads
/// the program and the weights and returns a plan. An exercise with no known weight comes back
/// with a null target, which is the screen's cue to ask for one.
/// </summary>
public static class LiftSessionBuilder
{
    public static LiftSessionPlan For(
        LiftProgram program,
        string dayKey,
        IReadOnlyDictionary<string, decimal> workingWeightsKg)
    {
        var day = program.Day(dayKey);

        var exercises = day.Exercises.Select(exercise =>
        {
            var scheme = program.SchemeFor(exercise.Tier);
            var sets = Enumerable.Range(0, scheme.Sets)
                .Select(index => new LiftSet(index, scheme.Reps, scheme.AmrapLast && index == scheme.Sets - 1))
                .ToArray();

            var target = workingWeightsKg.TryGetValue(exercise.ExerciseId, out var kg) ? kg
                : exercise.Loading == LoadingKind.Bodyweight ? 0m
                : (decimal?)null;

            return new LiftExercisePlan(
                exercise.ExerciseId,
                exercise.Tier,
                exercise.Loading,
                scheme,
                target,
                program.RestSecondsFor(exercise.Tier),
                sets);
        }).ToArray();

        return new LiftSessionPlan(program.Id, program.Name, day.Key, exercises);
    }
}
