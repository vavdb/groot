using Groot.Core.Equipment;
using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.UI.Gallery.Pages;

/// <summary>One plate on the stack: the denomination, and how many of them the session used.</summary>
public sealed record PlateCount(decimal Kg, int Count);

/// <summary>
/// The plates a finished session actually used, solved against the rack by
/// <see cref="PlateSolver"/> rather than invented. Two ways to count them, because they answer
/// different questions and the stack looks different for each.
/// </summary>
public static class SessionPlates
{
    /// <summary>
    /// Every plate that went on the bar, counted once per loading. This is what the lifter
    /// carried from the rack to the bar today: a heavier day shows heavier plates, and adding
    /// sets changes nothing, because the bar was already loaded.
    /// </summary>
    public static IReadOnlyList<PlateCount> Loaded(LiftSessionPlan plan, EquipmentProfile rack) =>
        Count(plan, rack, perSet: false);

    /// <summary>
    /// The same plates counted once per working set, so the stack grows with the work rather
    /// than with the weight alone. Five sets of 60 kg reads as five times the plates.
    /// </summary>
    public static IReadOnlyList<PlateCount> Handled(LiftSessionPlan plan, EquipmentProfile rack) =>
        Count(plan, rack, perSet: true);

    /// <summary>Total moved, in kilograms: every working set's weight times its target reps.</summary>
    public static decimal Volume(LiftSessionPlan plan) =>
        plan.Exercises.Sum(e => (e.TargetKg ?? 0m) * e.Sets.Sum(s => s.TargetReps));

    private static IReadOnlyList<PlateCount> Count(LiftSessionPlan plan, EquipmentProfile rack, bool perSet)
    {
        var tally = new Dictionary<decimal, int>();

        foreach (var exercise in plan.Exercises)
        {
            // Bodyweight lifts put nothing on a bar, and a lift with no weight yet has nothing
            // to solve, so neither contributes a plate.
            if (exercise.Loading != LoadingKind.Barbell || exercise.TargetKg is not { } target) continue;

            var perSide = rack.PerSide(target);
            if (perSide is null) continue;

            var loadings = perSet ? exercise.Sets.Count : 1;

            foreach (var plate in perSide)
            {
                // Plates load in pairs, so each one in the per-side breakdown is two off the rack.
                tally[plate] = tally.GetValueOrDefault(plate) + 2 * loadings;
            }
        }

        return tally
            .OrderByDescending(entry => entry.Key)
            .Select(entry => new PlateCount(entry.Key, entry.Value))
            .ToArray();
    }
}
