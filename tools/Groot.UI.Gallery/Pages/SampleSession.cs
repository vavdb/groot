using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.UI.Gallery.Pages;

/// <summary>
/// A GZCLP day for the recap concept, resolved a few months in rather than on day one. Opening
/// weights land on round numbers that need one plate a side, so a first session only ever shows
/// two denominations and the colour coding cannot be read. These are weights a lifter actually
/// arrives at, and they pull in the rest of the set.
/// </summary>
public static class SampleSession
{
    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");

    private static readonly Dictionary<ExerciseSlot, ExerciseState> Weights = new()
    {
        // A1 trains squat at T1 and bench at T2, so those two decide what the recap draws.
        [new("squat", 1)] = ExerciseState.Starting(100m),       // 45 a side: 25 + 20
        [new("squat", 2)] = ExerciseState.Starting(72.5m),
        [new("bench-press", 1)] = ExerciseState.Starting(80m),
        [new("bench-press", 2)] = ExerciseState.Starting(70m),  // 30 a side: 25 + 5
        [new("overhead-press", 1)] = ExerciseState.Starting(50m),
        [new("overhead-press", 2)] = ExerciseState.Starting(40m),
        [new("deadlift", 1)] = ExerciseState.Starting(130m),   // 60 a side: 25 + 20 + 15
        [new("deadlift", 2)] = ExerciseState.Starting(95m),
    };

    /// <summary>A1: squat at T1, bench at T2, chin-ups at T3.</summary>
    public static LiftSessionPlan A1 { get; } = LiftSessionBuilder.For(Gzclp, "A1", Weights);

    /// <summary>B2: the deadlift day, so the recap can be read at a heavier load.</summary>
    public static LiftSessionPlan B2 { get; } = LiftSessionBuilder.For(Gzclp, "B2", Weights);
}
