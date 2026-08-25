namespace Groot.Core.Equipment;

/// <summary>
/// What is available to load: one bar and the plates that go on it. Screens take a profile rather
/// than assuming a bar weight, because the bar in the room is rarely the bar in the assumption
/// (the owner's ATX is 11 kg, loaded as 10).
/// </summary>
public sealed record EquipmentProfile(Equipment Bar, IReadOnlyList<PlatePair> Plates)
{
    /// <summary>Every total this profile can build, ascending.</summary>
    public IReadOnlyList<decimal> AchievableTotals() => PlateSolver.AchievableTotals(Bar, Plates);

    /// <summary>The plates for one side of a total, heaviest first; null when it cannot be built.</summary>
    public IReadOnlyList<decimal>? PerSide(decimal totalKg) => PlateSolver.PerSideBreakdown(totalKg, Bar, Plates);

    /// <summary>The nearest total at or above <paramref name="targetKg"/> that the plates can build.</summary>
    public decimal? Round(decimal targetKg) => PlateSolver.RoundToAchievable(targetKg, AchievableTotals());

    /// <summary>
    /// The owner's rack, from design/habit-system.md §2: an ATX Professional 30 mm bar that weighs
    /// 11 kg and is loaded as 10, and the home plate set. This is a stand-in for the equipment
    /// profile that settings will own; it is here so exactly one place has to change when it does.
    /// </summary>
    public static EquipmentProfile Rack { get; } = new(
        new Equipment("atx", "ATX Professional 30mm", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 11m, CountsAsKg: 10m),
        [new(25m, 2), new(20m, 2), new(15m, 1), new(10m, 2), new(5m, 2), new(2.5m, 2), new(1.25m, 2)]);
}
