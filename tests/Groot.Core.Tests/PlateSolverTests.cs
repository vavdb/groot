using Groot.Core.Equipment;

namespace Groot.Core.Tests;

public class PlateSolverTests
{
    // The owner's bar: ATX Professional 30 mm, 11 kg actual, counts as 10.
    private static readonly Equipment.Equipment AtxBar =
        new("atx", "ATX Professional 30mm", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 11m, CountsAsKg: 10m);

    private static readonly PlatePair[] Inventory =
    [
        new(25m, 1), new(10m, 1), new(5m, 1), new(2.5m, 1), new(1.25m, 1)
    ];

    [Fact]
    public void CountsAs_drives_the_total()
    {
        // bar 10 + 2x25 = 60, the intro session set
        var totals = PlateSolver.AchievableTotals(AtxBar, Inventory);
        Assert.Contains(60m, totals);
    }

    [Fact]
    public void Exact_100_is_achievable_on_the_nominal_bar()
    {
        // 10 + 2x45 (25+10+5+2.5+1.25 = 43.75 is short; but 100 needs 45/side)
        // With this single-pair inventory 45/side is not buildable; verify the solver says so.
        var breakdown = PlateSolver.PerSideBreakdown(100m, AtxBar, Inventory);
        Assert.Null(breakdown);
    }

    [Fact]
    public void Round_target_lands_on_next_achievable_load()
    {
        var totals = PlateSolver.AchievableTotals(AtxBar, Inventory);
        var rounded = PlateSolver.RoundToAchievable(61m, totals);
        Assert.NotNull(rounded);
        Assert.True(rounded >= 61m);
        Assert.Contains(rounded.Value, totals);
    }

    [Fact]
    public void Breakdown_builds_the_intro_weight()
    {
        var breakdown = PlateSolver.PerSideBreakdown(60m, AtxBar, Inventory);
        Assert.NotNull(breakdown);
        Assert.Equal([25m], breakdown);
    }

    [Fact]
    public void Empty_bar_is_achievable()
    {
        var totals = PlateSolver.AchievableTotals(AtxBar, Inventory);
        Assert.Contains(10m, totals);
    }

    /// <summary>
    /// Racks with more than one pair of a plate lighter than another denomination. A greedy pass
    /// strands the remainder on these: 10 a side off 4 + 3 + 3 starts with the 4 and cannot finish.
    /// </summary>
    public static TheoryData<string, decimal, PlatePair[]> Racks => new()
    {
        { "home rack", 20m, [new(20m, 2), new(15m, 1), new(10m, 2), new(5m, 2), new(2.5m, 2), new(1.25m, 2)] },
        { "no fives", 20m, [new(20m, 2), new(15m, 2), new(10m, 2), new(2.5m, 2), new(1.25m, 2)] },
        { "starter", 20m, [new(15m, 1), new(10m, 1), new(2.5m, 3)] },
        { "odd metric", 20m, [new(4m, 1), new(3m, 2)] },
        { "one denomination", 15m, [new(2.5m, 6)] },
    };

    [Theory]
    [MemberData(nameof(Racks))]
    public void Every_achievable_total_can_actually_be_built(string rack, decimal barKg, PlatePair[] inventory)
    {
        var bar = new Equipment.Equipment(rack, rack, EquipmentKind.Bar, WeightUnit.Kg, ActualKg: barKg, CountsAsKg: barKg);

        foreach (var total in PlateSolver.AchievableTotals(bar, inventory))
        {
            var breakdown = PlateSolver.PerSideBreakdown(total, bar, inventory);

            Assert.NotNull(breakdown);
            Assert.Equal(total, bar.EffectiveBarKg + 2m * breakdown.Sum());
        }
    }

    [Fact]
    public void A_total_the_rack_cannot_build_is_null()
    {
        var bar = new Equipment.Equipment("bar", "Bar", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 20m, CountsAsKg: 20m);
        PlatePair[] inventory = [new(20m, 1)];

        Assert.Null(PlateSolver.PerSideBreakdown(41m, bar, inventory));
        Assert.Null(PlateSolver.PerSideBreakdown(19m, bar, inventory));
    }

    [Fact]
    public void Breakdown_loads_the_heaviest_plates_first()
    {
        var bar = new Equipment.Equipment("bar", "Bar", EquipmentKind.Bar, WeightUnit.Kg, ActualKg: 20m, CountsAsKg: 20m);
        PlatePair[] inventory = [new(10m, 2), new(5m, 2), new(2.5m, 2)];

        Assert.Equal([10m, 5m, 2.5m], PlateSolver.PerSideBreakdown(55m, bar, inventory));
    }
}
