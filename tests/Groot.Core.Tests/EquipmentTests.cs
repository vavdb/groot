using Groot.Core.Equipment;
using Groot.Core.Sessions;

namespace Groot.Core.Tests;

/// <summary>
/// The unit and the bar weight live on the equipment, never on the app (AGENTS.md). These are the
/// conversions every logged set passes through, so they get the same scrutiny as the engines.
/// </summary>
public class EquipmentTests
{
    [Fact]
    public void A_nominal_weight_wins_over_the_measured_one()
    {
        // The owner's ATX bar weighs 11 and is loaded as 10, so plate maths stays round.
        var atx = new Equipment.Equipment("atx", "ATX 30mm", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 11m, CountsAsKg: 10m);

        Assert.Equal(10m, atx.EffectiveBarKg);
    }

    [Fact]
    public void A_bar_with_only_a_measured_weight_uses_it()
    {
        var bar = new Equipment.Equipment("b", "Bar", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 20m);

        Assert.Equal(20m, bar.EffectiveBarKg);
    }

    [Fact]
    public void A_bar_with_no_weight_at_all_counts_as_nothing()
    {
        // Zero, not a 20 kg assumption: an unweighed bar is a data gap, and pretending otherwise
        // is the mistake AGENTS.md names.
        var unweighed = new Equipment.Equipment("b", "Bar", EquipmentKind.Bar, WeightUnit.Kg);

        Assert.Equal(0m, unweighed.EffectiveBarKg);
    }

    [Theory]
    [InlineData(45, 20.41165665)]
    [InlineData(2.5, 1.133980925)]
    public void Pounds_convert_to_kilos(double lb, double expectedKg) =>
        Assert.Equal((decimal)expectedKg, Units.LbToKg((decimal)lb), 6);

    [Fact]
    public void The_two_conversions_are_each_other_inverse()
    {
        var kg = Units.LbToKg(45m);

        Assert.Equal(45m, Units.KgToLb(kg), 6);
    }
}

/// <summary>
/// One logged set, in the three ways a weight is actually entered. <c>WeightKg</c> is canonical
/// (what moved, in total); <c>EntryWeight</c> and <c>EntryUnit</c> keep what the lifter typed.
/// </summary>
public class SetEntryTests
{
    private static readonly Guid Workout = Guid.NewGuid();

    private static readonly Equipment.Equipment Atx =
        new("atx", "ATX 30mm", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 11m, CountsAsKg: 10m);

    [Fact]
    public void Per_side_entry_totals_the_nominal_bar_plus_both_sides()
    {
        var set = SetEntry.PerSide(Workout, "squat", 1, Atx, sideKg: 25m, reps: 3);

        Assert.Equal(60m, set.WeightKg);
        Assert.Equal(25m, set.EntryWeight);
        Assert.Equal(EntryMode.PerSide, set.Mode);
        Assert.Equal("atx", set.EquipmentId);
    }

    [Fact]
    public void Total_entry_is_stored_as_typed()
    {
        var set = SetEntry.Total(Workout, "leg-press", 1, totalKg: 90m, reps: 10);

        Assert.Equal(90m, set.WeightKg);
        Assert.Equal(90m, set.EntryWeight);
        Assert.Equal(EntryMode.Total, set.Mode);
        Assert.Null(set.EquipmentId);
    }

    [Fact]
    public void Per_hand_entry_counts_both_hands_and_keeps_the_dumbbell_unit()
    {
        var powerBlock = new Equipment.Equipment("pb", "PowerBlock", EquipmentKind.AdjustableDumbbell, WeightUnit.Lb);

        var set = SetEntry.PerHand(Workout, "dumbbell-row", 1, powerBlock, perHand: 35m, reps: 12);

        // 35 lb in each hand: canonical kg is both hands, the entry stays 35 lb.
        Assert.Equal(2m * Units.LbToKg(35m), set.WeightKg, 6);
        Assert.Equal(35m, set.EntryWeight);
        Assert.Equal(WeightUnit.Lb, set.EntryUnit);
        Assert.Equal(EntryMode.PerHand, set.Mode);
    }

    [Fact]
    public void A_kilo_dumbbell_needs_no_conversion()
    {
        var dumbbell = new Equipment.Equipment("db", "Dumbbell", EquipmentKind.FixedDumbbell, WeightUnit.Kg);

        var set = SetEntry.PerHand(Workout, "dumbbell-curl", 1, dumbbell, perHand: 12.5m, reps: 10);

        Assert.Equal(25m, set.WeightKg);
    }

    [Fact]
    public void Every_entry_mode_can_log_a_set_without_reps()
    {
        // Reps are nullable: a set can be logged before it is finished.
        Assert.Null(SetEntry.Total(Workout, "squat", 1, 60m, reps: null).Reps);
    }
}
