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
    }

    [Fact]
    public void every_exercise_says_how_it_is_loaded()
    {
        Assert.Equal(
            [LoadingKind.Barbell, LoadingKind.Barbell, LoadingKind.Bodyweight],
            Gzclp.Day("A1").Exercises.Select(e => e.Loading));

        Assert.Equal(
            [LoadingKind.Barbell, LoadingKind.Barbell, LoadingKind.Dumbbell],
            Gzclp.Day("B1").Exercises.Select(e => e.Loading));
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

public class LiftProgramParsingTests
{
    private const string Minimal = """
        {
          "id": "test", "name": "Test", "type": "sets_reps",
          "rotation": ["A"],
          "progression": { "T1": { "scheme": "5x3+", "incrementKg": { "default": 2.5 } } },
          "days": { "A": [ { "exercise": "squat", "tier": 1, "loading": "barbell" } ] }
        }
        """;

    [Fact]
    public void a_day_may_not_name_the_same_exercise_twice()
    {
        var json = Minimal.Replace(
            """[ { "exercise": "squat", "tier": 1, "loading": "barbell" } ]""",
            """[ { "exercise": "squat", "tier": 1, "loading": "barbell" }, { "exercise": "squat", "tier": 2, "loading": "barbell" } ]""");

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));

        Assert.Contains("more than once", error.Message);
    }

    [Fact]
    public void a_tier_that_declares_increments_must_name_a_default()
    {
        var json = Minimal.Replace("""{ "default": 2.5 }""", """{ "deadlift": 5.0 }""");

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));

        Assert.Contains("without a default", error.Message);
    }

    [Fact]
    public void an_unknown_loading_is_refused()
    {
        var json = Minimal.Replace("\"loading\": \"barbell\"", "\"loading\": \"kettlebell\"");

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));

        Assert.Contains("expected barbell, dumbbell or bodyweight", error.Message);
    }

    [Fact]
    public void the_minimal_shape_parses()
    {
        var catalog = ProgramCatalog.Parse([Minimal]);

        var program = catalog.LiftProgram("test");
        Assert.Equal(["A"], program.Rotation);
        Assert.Equal(2.5m, program.TierFor(1).IncrementFor("squat"));
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
    public void weights_come_from_the_lifter_and_a_missing_barbell_weight_is_left_open()
    {
        var plan = LiftSessionBuilder.For(Gzclp, "B1", new Dictionary<string, decimal> { ["deadlift"] = 100m });

        Assert.Null(plan.Exercises[0].TargetKg);
        Assert.Equal(100m, plan.Exercises[1].TargetKg);
    }

    [Fact]
    public void a_bodyweight_lift_starts_at_no_adjustment_rather_than_unknown()
    {
        var chinUp = LiftSessionBuilder.For(Gzclp, "A1", Weights).Exercises[2];

        Assert.Equal(LoadingKind.Bodyweight, chinUp.Loading);
        Assert.Equal(0m, chinUp.TargetKg);
    }

    [Fact]
    public void the_plan_carries_the_loading_of_each_exercise()
    {
        var plan = LiftSessionBuilder.For(Gzclp, "B1", Weights);

        Assert.Equal(
            [LoadingKind.Barbell, LoadingKind.Barbell, LoadingKind.Dumbbell],
            plan.Exercises.Select(e => e.Loading));
    }

    [Fact]
    public void rest_follows_the_tier()
    {
        var plan = LiftSessionBuilder.For(Gzclp, "A1", Weights);

        Assert.Equal([180, 120, 60], plan.Exercises.Select(e => e.RestSeconds));
    }
}

public class LiftProgressionPlannerTests
{
    private static LiftProgram Gzclp => ProgramCatalog.Embedded.LiftProgram("gzclp-rack");

    private static ExerciseOutcome Squat(bool completed, int stage = 0, decimal weight = 60m, int reps = 15) =>
        new("squat", Tier: 1, weight, stage, completed, reps);

    [Fact]
    public void tiers_carry_their_increments_and_ladder()
    {
        var t1 = Gzclp.TierFor(1);

        Assert.Equal(2.5m, t1.IncrementFor("squat"));
        Assert.Equal(5.0m, t1.IncrementFor("deadlift"));
        Assert.Equal([new SetScheme(6, 2, true), new SetScheme(10, 1, true)], t1.FailLadder);
        Assert.Equal(0.9m, t1.ResetPctOfLast);
    }

    [Fact]
    public void a_clean_session_adds_the_increment_and_keeps_the_scheme()
    {
        var next = LiftProgressionPlanner.Next(Gzclp, Squat(completed: true));

        Assert.Equal(62.5m, next.WeightKg);
        Assert.Equal(new SetScheme(5, 3, true), next.Scheme);
        Assert.Equal(0, next.Stage);
    }

    [Fact]
    public void a_deadlift_climbs_by_its_own_increment()
    {
        var outcome = new ExerciseOutcome("deadlift", Tier: 1, WeightKg: 100m, Stage: 0, AllSetsCompleted: true, TotalReps: 15);

        Assert.Equal(105m, LiftProgressionPlanner.Next(Gzclp, outcome).WeightKg);
    }

    [Fact]
    public void a_failed_session_keeps_the_weight_and_drops_a_rung()
    {
        var next = LiftProgressionPlanner.Next(Gzclp, Squat(completed: false));

        Assert.Equal(60m, next.WeightKg);
        Assert.Equal(new SetScheme(6, 2, true), next.Scheme);
        Assert.Equal(1, next.Stage);
    }

    [Fact]
    public void failing_the_last_rung_resets_to_ninety_percent_and_the_first_scheme()
    {
        var next = LiftProgressionPlanner.Next(Gzclp, Squat(completed: false, stage: 2));

        Assert.Equal(54m, next.WeightKg);
        Assert.Equal(new SetScheme(5, 3, true), next.Scheme);
        Assert.Equal(0, next.Stage);
    }

    [Fact]
    public void a_failed_t2_ends_its_ladder_by_adding_weight_and_starting_again()
    {
        var outcome = new ExerciseOutcome("bench-press", Tier: 2, WeightKg: 45m, Stage: 2, AllSetsCompleted: false, TotalReps: 20);

        var next = LiftProgressionPlanner.Next(Gzclp, outcome);

        Assert.Equal(47.5m, next.WeightKg);
        Assert.Equal(new SetScheme(3, 10, false), next.Scheme);
        Assert.Equal(0, next.Stage);
    }

    [Theory]
    [InlineData(25, 22.5)]
    [InlineData(45, 22.5)]
    [InlineData(24, 20.0)]
    public void a_t3_climbs_only_once_the_reps_add_up(int totalReps, double expected)
    {
        var outcome = new ExerciseOutcome("dumbbell-row", Tier: 3, WeightKg: 20m, Stage: 0, AllSetsCompleted: true, totalReps);

        Assert.Equal((decimal)expected, LiftProgressionPlanner.Next(Gzclp, outcome).WeightKg);
    }
}
