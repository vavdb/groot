using Groot.Core.Intervals;
using Groot.Core.Programs;

namespace Groot.Core.Tests;

public class ProgramCatalogTests
{
    private static readonly IntervalProgram Couch = ProgramCatalog.Embedded.IntervalProgram("0-to-5k");

    [Fact]
    public void Embedded_catalog_contains_both_shipped_programs()
    {
        // New programs are new data files, never code; assert membership, not the exact roster.
        var ids = ProgramCatalog.Embedded.Programs.Select(p => p.Id).ToArray();
        Assert.Contains("0-to-5k", ids);
        Assert.Contains("gzclp-rack", ids);
    }

    [Fact]
    public void Lifting_program_is_listed_but_is_not_an_interval_program()
    {
        var gzclp = ProgramCatalog.Embedded.Programs.Single(p => p.Id == "gzclp-rack");
        Assert.Equal(ProgramType.SetsReps, gzclp.Type);
        Assert.DoesNotContain(ProgramCatalog.Embedded.IntervalPrograms, p => p.Id == "gzclp-rack");
    }

    [Fact]
    public void Couch_program_has_nine_weeks_and_twentyseven_sessions()
    {
        Assert.Equal("0→5K", Couch.Name);
        Assert.Equal(Enumerable.Range(1, 9), Couch.WeekNumbers);
        Assert.Equal(27, Couch.TotalSessions);
    }

    [Fact]
    public void Cue_defaults_come_from_the_definition()
    {
        Assert.True(Couch.CueDefaults.SegmentStartCue);
        Assert.Equal(-10, Couch.CueDefaults.EndingSoonCueAtSeconds);
    }

    [Fact]
    public void Uniform_week_repeats_one_plan_for_every_session()
    {
        var week3 = Couch.Week(3);
        Assert.Equal([1, 2, 3], week3.DayNumbers);
        Assert.Same(week3.PlanFor(1), week3.PlanFor(3));
    }

    [Fact]
    public void Week_three_matches_the_published_shape()
    {
        var plan = Couch.Week(3).PlanFor(1);

        Assert.Equal(10, plan.Count);
        Assert.Equal(SegmentKind.Walk, plan[0].Kind);
        Assert.Equal(300, plan[0].Seconds);
        Assert.Equal("warmup", plan[0].Label);
        Assert.Equal([new CuePoint(-15, "cue.warmupEnding")], plan[0].Cues);
        Assert.Equal([90, 90, 180, 180, 90, 90, 180, 180], plan.Skip(1).SkipLast(1).Select(s => s.Seconds));
        Assert.Equal("cooldown", plan[^1].Label);
        Assert.Equal(1680, plan.Sum(s => s.Seconds)); // 28 minutes
    }

    [Fact]
    public void Days_override_week_gives_each_session_its_own_plan()
    {
        var week5 = Couch.Week(5);

        Assert.Equal([1, 2, 3], week5.DayNumbers);
        Assert.Equal([300, 300, 180, 300, 180, 300, 300], week5.PlanFor(1).Select(s => s.Seconds));
        Assert.Equal([300, 480, 300, 480, 300], week5.PlanFor(2).Select(s => s.Seconds));
        Assert.Equal(1200, week5.PlanFor(3).Single(s => s.Kind == SegmentKind.Run).Seconds);
    }

    [Fact]
    public void Unknown_day_in_an_override_week_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Couch.Week(5).PlanFor(4));
    }

    [Fact]
    public void Unknown_week_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Couch.Week(12));
    }

    [Fact]
    public void Declared_cues_survive_parsing()
    {
        var longRun = Couch.Week(9).PlanFor(1)[1];
        Assert.Equal([new CuePoint(900, "cue.halfway"), new CuePoint(-60, "cue.finalMinute")], longRun.Cues);
    }

    [Fact]
    public void Week_with_both_plan_and_days_is_rejected()
    {
        const string json = """
        { "id": "bad", "version": 1, "name": "Bad", "type": "intervals",
          "weeks": [ { "week": 1, "sessionsPerWeek": 3,
            "plan": [ { "kind": "run", "seconds": 60 } ],
            "days": [ { "day": 1, "plan": [ { "kind": "run", "seconds": 60 } ] } ] } ] }
        """;

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));
        Assert.Contains("exactly one of 'plan' or 'days'", error.Message);
    }

    [Fact]
    public void Unknown_segment_kind_is_rejected()
    {
        const string json = """
        { "id": "bad", "version": 1, "name": "Bad", "type": "intervals",
          "weeks": [ { "week": 1, "plan": [ { "kind": "swim", "seconds": 60 } ] } ] }
        """;

        var error = Assert.Throws<InvalidOperationException>(() => ProgramCatalog.Parse([json]));
        Assert.Contains("unknown segment kind 'swim'", error.Message);
    }

    [Fact]
    public void Missing_program_id_is_rejected()
    {
        var error = Assert.Throws<KeyNotFoundException>(() => ProgramCatalog.Embedded.IntervalProgram("nope"));
        Assert.Contains("nope", error.Message);
    }
}
