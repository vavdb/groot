using Groot.Core.Programs;

namespace Groot.Core.Tests;

/// <summary>
/// The parser is the boundary between hand-authored program files and every engine downstream, so
/// each way a file can be wrong gets a test that names the wrongness. Each case starts from a
/// minimal valid program and breaks exactly one thing.
/// </summary>
public class ProgramCatalogFailureTests
{
    private const string ValidLift = """
        {
          "id": "lift", "name": "Lift", "type": "sets_reps",
          "rotation": ["A"],
          "sessionsPerWeek": 3,
          "restSeconds": { "1": 180 },
          "progression": { "T1": { "scheme": "5x3+", "incrementKg": { "default": 2.5 } } },
          "days": { "A": [ { "exercise": "squat", "tier": 1, "loading": "barbell" } ] }
        }
        """;

    private const string ValidInterval = """
        {
          "id": "run", "name": "Run", "type": "intervals",
          "weeks": [ { "week": 1, "sessionsPerWeek": 3,
                       "plan": [ { "kind": "walk", "seconds": 300 }, { "kind": "run", "seconds": 60 } ] } ]
        }
        """;

    private static string Break(string json, string find, string replace)
    {
        var broken = json.Replace(find, replace);
        Assert.NotEqual(json, broken);
        return broken;
    }

    public static TheoryData<string, string, string, string> LiftFaults => new()
    {
        { "negative increment", "\"default\": 2.5", "\"default\": -2.5", "increments by" },
        { "negative rest", "\"1\": 180", "\"1\": -30", "rests" },
        { "no sessions per week", "\"sessionsPerWeek\": 3", "\"sessionsPerWeek\": 0", "must be positive" },
        { "rotation names an unknown day", "\"rotation\": [\"A\"]", "\"rotation\": [\"A\", \"Z\"]", "not a day" },
        { "a day with no exercises", "[ { \"exercise\": \"squat\", \"tier\": 1, \"loading\": \"barbell\" } ]", "[]", "no exercises" },
        { "a tier key that is not TN", "\"T1\":", "\"T9x\":", "expected T1, T2 or T3" },
        { "a tier out of range", "\"tier\": 1", "\"tier\": 4", "tiers are 1 to 3" },
        { "an unknown loading", "\"loading\": \"barbell\"", "\"loading\": \"kettlebell\"", "expected barbell, dumbbell or bodyweight" },
        { "a scheme that is not a scheme", "\"scheme\": \"5x3+\"", "\"scheme\": \"five by three\"", "" },
        { "the retired total-reps threshold", "\"incrementKg\"", "\"progressAtTotalReps\": 25, \"incrementKg\"", "progressAtAmrapReps" },
    };

    [Theory]
    [MemberData(nameof(LiftFaults))]
    public void A_broken_lift_program_says_what_is_broken(string fault, string find, string replace, string expected)
    {
        var json = Break(ValidLift, find, replace);

        var error = Record.Exception(() => ProgramCatalog.Parse([json]));

        Assert.True(error is not null, $"{fault} parsed without complaint");
        if (expected.Length > 0) Assert.Contains(expected, error.Message);
    }

    public static TheoryData<string, string, string, string> IntervalFaults => new()
    {
        { "a segment of no length", "\"seconds\": 300", "\"seconds\": 0", "has a segment of" },
        { "an unknown segment kind", "\"kind\": \"walk\"", "\"kind\": \"sprint\"", "unknown segment kind" },
        { "a week numbered zero", "\"week\": 1", "\"week\": 0", "weeks must be positive" },
        { "no sessions in a week", "\"sessionsPerWeek\": 3", "\"sessionsPerWeek\": 0", "must be positive" },
    };

    [Theory]
    [MemberData(nameof(IntervalFaults))]
    public void A_broken_interval_program_says_what_is_broken(string fault, string find, string replace, string expected)
    {
        var json = Break(ValidInterval, find, replace);

        var error = Record.Exception(() => ProgramCatalog.Parse([json]));

        Assert.True(error is not null, $"{fault} parsed without complaint");
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void A_week_with_neither_a_plan_nor_days_is_refused()
    {
        var json = ValidInterval.Replace(
            "\"plan\": [ { \"kind\": \"walk\", \"seconds\": 300 }, { \"kind\": \"run\", \"seconds\": 60 } ]",
            "\"cueDefaults\": {}");

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));

        Assert.Contains("exactly one of 'plan' or 'days'", error.Message);
    }

    [Fact]
    public void A_cue_outside_its_segment_is_refused()
    {
        var json = ValidInterval.Replace(
            "{ \"kind\": \"run\", \"seconds\": 60 }",
            "{ \"kind\": \"run\", \"seconds\": 60, \"cues\": [ { \"at\": 90, \"key\": \"halfway\" } ] }");

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));

        Assert.Contains("outside a 60s segment", error.Message);
    }

    [Fact]
    public void An_unknown_program_type_is_refused()
    {
        var json = ValidLift.Replace("\"type\": \"sets_reps\"", "\"type\": \"circuit\"");

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));

        Assert.Contains("unknown type", error.Message);
    }

    [Fact]
    public void The_valid_fixtures_really_are_valid()
    {
        // Otherwise every case above could pass for the wrong reason.
        var catalog = ProgramCatalog.Parse([ValidLift, ValidInterval]);

        Assert.Equal(["lift", "run"], catalog.Programs.Select(p => p.Id));
    }
}
