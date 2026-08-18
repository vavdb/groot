namespace Groot.Core.Equipment;

public enum WeightUnit { Kg, Lb }

public enum EquipmentKind { Bar, AdjustableDumbbell, FixedDumbbell, Kettlebell, Other }

/// <summary>
/// A piece of loadable equipment. Unit lives here, not on the app.
/// Bars may carry a nominal weight (<see cref="CountsAsKg"/>) used for totals, targets and
/// milestones, while <see cref="ActualKg"/> preserves the measured weight.
/// </summary>
public sealed record Equipment(
    string Id,
    string Name,
    EquipmentKind Kind,
    WeightUnit Unit,
    decimal? ActualKg = null,
    decimal? CountsAsKg = null,
    IReadOnlyList<decimal>? DeclaredLoads = null)
{
    public decimal EffectiveBarKg => CountsAsKg ?? ActualKg ?? 0m;
}

public static class Units
{
    public const decimal KgPerLb = 0.45359237m;

    public static decimal LbToKg(decimal lb) => lb * KgPerLb;
    public static decimal KgToLb(decimal kg) => kg / KgPerLb;
}
