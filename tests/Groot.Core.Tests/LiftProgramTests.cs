using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.Core.Tests;

public class SetSchemeTests
{
    [Theory]
    [InlineData("5x3+", 5, 3, true)]
    [InlineData("3x10", 3, 10, false)]
    [InlineData("10x1+", 10, 1, true)]
    [InlineData(" 3x15+ ", 3, 15, true)]
    public void parses_the_forms_programs_write(string text, int sets, int reps, bool amrap)
    {
        var scheme = SetScheme.Parse(text);

        Assert.Equal(sets, scheme.Sets);
        Assert.Equal(reps, scheme.Reps);
        Assert.Equal(amrap, scheme.AmrapLast);
    }

    [Theory]
    [InlineData("five by three")]
    [InlineData("5")]
    [InlineData("0x3")]
    [InlineData("5x0")]
    [InlineData("5x3x2")]
    public void refuses_anything_else(string text) =>
        Assert.Throws<FormatException>(() => SetScheme.Parse(text));

    [Fact]
    public void round_trips_through_its_text() =>
        Assert.Equal("5x3+", SetScheme.Parse("5x3+").Text);
}

public class LiftCatalogTests
{
    private static LiftProgram Gzclp => ProgramCatalog.Embedded.LiftProgram("gzclp-rack");

    [Fact]
    public void embedded_catalog_holds_the_lift_program()
    {
        Assert.Contains(ProgramCatalog.Embedded.LiftPrograms, p => p.Id == "gzclp-rack");
        Assert.Equal("GZCLP (rack edition)", Gzclp.Name);
        Assert.Equal(3, Gzclp.SessionsPerWeek);
    }

    [Fact]
    public void rotation_names_days_that_exist()
    {
        Assert.Equal(["A1", "B1", "A2", "B2"], Gzclp.Rotation);
        Assert.All(Gzclp.Rotation, key => Assert.NotNull(Gzclp.Day(key)));
    }

    [Fact]
    public void rotation_wraps_in_both_directions()
    {
        Assert.Equal("A1", Gzclp.DayAt(0).Key);
        Assert.Equal("A1", Gzclp.DayAt(4).Key);
        Assert.Equal("B2", Gzclp.DayAt(-1).Key);
    }

    [Fact]
    public void a_day_holds_its_exercises_in_order()
    {
        var day = Gzclp.Day("A1");

        Assert.Equal(["squat", "bench-press", "chin-up"], day.Exercises.Select(e => e.ExerciseId));
        Assert.Equal([1, 2, 3], day.Exercises.Select(e => e.Tier));
        Assert.Equal("bodyweight+", day.Exercises[2].Loading);
    }

    [Fact]
    public void tiers_carry_their_scheme_and_rest()
    {
        Assert.Equal(new SetScheme(5, 3, true), Gzclp.SchemeFor(1));
        Assert.Equal(new SetScheme(3, 10, false), Gzclp.SchemeFor(2));
        Assert.Equal(new SetScheme(3, 15, true), Gzclp.SchemeFor(3));

        Assert.Equal(180, Gzclp.RestSecondsFor(1));
        Assert.Equal(60, Gzclp.RestSecondsFor(3));
    }

    [Fact]
    public void an_undeclared_rest_falls_back_rather_than_throwing()
    {
        var program = Gzclp with { RestSeconds = new Dictionary<int, int>() };

        Assert.Equal(90, program.RestSecondsFor(1));
    }

    [Fact]
    public void an_unknown_day_or_tier_says_so() 
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Gzclp.Day("C9"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Gzclp.SchemeFor(4));
    }
}

public class LiftSessionBuilderTests
{
    private static LiftProgram Gzclp => ProgramCatalog.Embedded.LiftProgram("gzclp-rack");

    private static readonly Dictionary<string, decimal> Weights = new()
    {
        ["squat"] = 60m,
        ["bench-press"] = 45m,
    };

    [Fact]
    public void builds_every_set_the_day_asks_for()
    {
        var plan = LiftSessionBuilder.For(Gzclp, "A1", Weights);

        Assert.Equal("A1", plan.DayKey);
        Assert.Equal(5 + 3 + 3, plan.TotalSets);
        Assert.Equal(["squat", "bench-press", "chin-up"], plan.Exercises.Select(e => e.ExerciseId));
    }

    [Fact]
    public void only_the_last_set_of_an_amrap_scheme_is_the_amrap()
    {
        var squat = LiftSessionBuilder.For(Gzclp, "A1", Weights).Exercises[0];

        Assert.Equal(5, squat.Sets.Count);
        Assert.All(squat.Sets, set => Assert.Equal(3, set.TargetReps));
        Assert.Equal([false, false, false, false, true], squat.Sets.Select(s => s.IsAmrap));
        Assert.Equal(15, squat.PlannedReps);
    }

    [Fact]
    public void a_straight_scheme_has_no_amrap()
    {
        var bench = LiftSessionBuilder.For(Gzclp, "A1", Weights).Exercises[1];

        Assert.All(bench.Sets, set => Assert.False(set.IsAmrap));
        Assert.Equal(30, bench.PlannedReps);
    }

    [Fact]
    public void weights_come_from_the_lifter_and_a_missing_one_is_left_open()
    {
        var plan = LiftSessionBuilder.For(Gzclp, "A1", Weights);

        Assert.Equal(60m, plan.Exercises[0].TargetKg);
        Assert.Equal(45m, plan.Exercises[1].TargetKg);
        Assert.Null(plan.Exercises[2].TargetKg);
    }

    [Fact]
    public void rest_follows_the_tier()
    {
        var plan = LiftSessionBuilder.For(Gzclp, "A1", Weights);

        Assert.Equal([180, 120, 60], plan.Exercises.Select(e => e.RestSeconds));
    }
}
