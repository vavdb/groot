using Groot.Core.Equipment;

namespace Groot.Core.Sessions;

public enum EntryMode { PerSide, Total, PerHand }

/// <summary>
/// One logged set. <see cref="WeightKg"/> is the canonical (nominal) total in kg;
/// <see cref="EntryWeight"/>/<see cref="EntryUnit"/> preserve what the user actually picked.
/// </summary>
public sealed record SetEntry(
    Guid Id,
    Guid WorkoutId,
    string ExerciseId,
    int SetOrder,
    decimal WeightKg,
    int? Reps,
    EntryMode Mode,
    decimal EntryWeight,
    WeightUnit EntryUnit,
    string? EquipmentId = null,
    bool IsWarmup = false,
    string? Notes = null)
{
    /// <summary>Per-side entry on a bar: total = counts-as bar weight + 2 × side.</summary>
    public static SetEntry PerSide(Guid workoutId, string exerciseId, int order,
        Equipment.Equipment bar, decimal sideKg, int? reps) =>
        new(Guid.NewGuid(), workoutId, exerciseId, order,
            WeightKg: bar.EffectiveBarKg + 2m * sideKg,
            Reps: reps, Mode: EntryMode.PerSide,
            EntryWeight: sideKg, EntryUnit: WeightUnit.Kg, EquipmentId: bar.Id);

    /// <summary>Plain total entry ("just 90").</summary>
    public static SetEntry Total(Guid workoutId, string exerciseId, int order,
        decimal totalKg, int? reps) =>
        new(Guid.NewGuid(), workoutId, exerciseId, order,
            WeightKg: totalKg, Reps: reps, Mode: EntryMode.Total,
            EntryWeight: totalKg, EntryUnit: WeightUnit.Kg);

    /// <summary>Per-hand dumbbell entry in the equipment's own unit (e.g. PowerBlock lb).</summary>
    public static SetEntry PerHand(Guid workoutId, string exerciseId, int order,
        Equipment.Equipment dumbbell, decimal perHand, int? reps)
    {
        var perHandKg = dumbbell.Unit == WeightUnit.Lb ? Units.LbToKg(perHand) : perHand;
        return new(Guid.NewGuid(), workoutId, exerciseId, order,
            WeightKg: 2m * perHandKg, Reps: reps, Mode: EntryMode.PerHand,
            EntryWeight: perHand, EntryUnit: dumbbell.Unit, EquipmentId: dumbbell.Id);
    }
}
