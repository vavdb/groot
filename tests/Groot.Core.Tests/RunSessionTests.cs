using Groot.Core.Intervals;
using Groot.Core.Programs;

namespace Groot.Core.Tests;

public class RunSessionTests
{
    private static readonly IntervalProgram Couch = ProgramCatalog.Embedded.IntervalProgram("0-to-5k");

    // warmup 300 · run 90 · walk 90 · cooldown 300 — the week 3 shape, one block
    private static RunSession Short() => new(
    [
        new Segment(SegmentKind.Walk, 300, "warmup", [new CuePoint(-15, "cue.warmupEnding")]),
        new Segment(SegmentKind.Run, 90),
        new Segment(SegmentKind.Walk, 90),
        new Segment(SegmentKind.Walk, 300, "cooldown"),
    ]);

    [Fact]
    public void Session_totals_come_from_the_segments()
    {
        var session = Short();
        Assert.Equal(780, session.TotalSeconds);
        Assert.Equal(1, session.RunCount);
        Assert.Equal(90, session.RunSeconds);
    }

    [Fact]
    public void Progress_reports_current_and_next_segment()
    {
        var progress = Short().ProgressAt(310);

        Assert.Equal(1, progress.SegmentIndex);
        Assert.Equal(SegmentKind.Run, progress.Segment.Kind);
        Assert.Equal(10, progress.ElapsedInSegment);
        Assert.Equal(80, progress.RemainingInSegment);
        Assert.Equal(SegmentKind.Walk, progress.NextSegment?.Kind);
        Assert.Equal(1, progress.RunOrdinal);
        Assert.False(progress.Finished);
    }

    [Fact]
    public void Progress_at_the_end_is_finished_and_has_no_next_segment()
    {
        var progress = Short().ProgressAt(780);

        Assert.True(progress.Finished);
        Assert.Equal(0, progress.RemainingTotal);
        Assert.Null(progress.NextSegment);
    }

    [Fact]
    public void Segment_start_cue_fires_at_second_zero()
    {
        var cues = Short().CuesBetween(-1, 0);

        var cue = Assert.Single(cues);
        Assert.Equal("cue.startWalk", cue.Key);
        Assert.Equal(RunCueKind.SegmentStart, cue.CueKind);
        Assert.Equal("warmup", cue.SegmentLabel);
    }

    [Fact]
    public void Ending_soon_cue_fires_ten_seconds_before_each_segment_end()
    {
        var cue = Assert.Single(Short().CuesBetween(289, 290));

        Assert.Equal("cue.endingSoon", cue.Key);
        Assert.Equal(10, cue.RemainingInSegment);
        Assert.Equal(SegmentKind.Run, cue.NextKind);
        Assert.Equal(90, cue.NextSeconds);
    }

    [Fact]
    public void Declared_cue_keeps_its_key_and_knows_which_run_is_next()
    {
        var cue = Assert.Single(Short().CuesBetween(284, 285));

        Assert.Equal("cue.warmupEnding", cue.Key);
        Assert.Equal(RunCueKind.Declared, cue.CueKind);
        Assert.Equal(1, cue.NextRunOrdinal);
    }

    [Fact]
    public void Cues_do_not_fire_twice()
    {
        var session = Short();
        Assert.Single(session.CuesBetween(284, 285));
        Assert.Empty(session.CuesBetween(285, 289));
    }

    [Fact]
    public void Clock_jump_collects_every_cue_in_the_window_once_and_in_order()
    {
        // app suspended over the warmup/run boundary: -15 declared, -10 ending soon, run start at 300
        var keys = Short().CuesBetween(280, 305).Select(c => c.Key).ToArray();

        Assert.Equal(["cue.warmupEnding", "cue.endingSoon", "cue.startRun"], keys);
    }

    [Fact]
    public void Short_segments_get_no_ending_soon_cue()
    {
        var session = new RunSession([new Segment(SegmentKind.Run, 10), new Segment(SegmentKind.Walk, 60)]);

        Assert.DoesNotContain(session.Cues, c => c.CueKind == RunCueKind.EndingSoon && c.SegmentIndex == 0);
        Assert.Contains(session.Cues, c => c.CueKind == RunCueKind.EndingSoon && c.SegmentIndex == 1);
    }

    [Fact]
    public void Session_ends_with_a_finished_cue()
    {
        var cue = Short().Cues[^1];

        Assert.Equal("cue.finished", cue.Key);
        Assert.Equal(780, cue.AtSecond);
        Assert.Equal(0, cue.RemainingTotal);
    }

    [Fact]
    public void Automatic_start_cues_can_be_switched_off()
    {
        var session = new RunSession(
            [new Segment(SegmentKind.Run, 120, null, [new CuePoint(60, "cue.halfway")])],
            new CueDefaults(SegmentStartCue: false, EndingSoonCueAtSeconds: 0));

        Assert.Equal(["cue.halfway", "cue.finished"], session.Cues.Select(c => c.Key));
    }

    [Fact]
    public void Skip_jumps_to_the_start_of_the_next_segment()
    {
        var session = Short();

        Assert.Equal(300, session.SkipTargetFrom(12));
        Assert.Equal(390, session.SkipTargetFrom(300));
        Assert.Equal(780, session.SkipTargetFrom(700)); // skipping the last segment finishes
    }

    [Fact]
    public void Week_and_day_selection_uses_the_uniform_plan()
    {
        var session = RunSession.For(Couch, week: 3, day: 2);

        Assert.Equal(new RunSessionId("0-to-5k", "0→5K", 3, 2), session.Id);
        Assert.Equal(1680, session.TotalSeconds);
        Assert.Equal(4, session.RunCount);
        Assert.Equal(540, session.RunSeconds);
        Assert.Equal(10, session.Segments.Count);
    }

    [Fact]
    public void Week_three_run_blocks_alternate_ninety_and_three_minutes()
    {
        var session = RunSession.For(Couch, week: 3, day: 1);
        var runs = session.Segments.Where(s => s.Kind == SegmentKind.Run).Select(s => s.Seconds);

        Assert.Equal([90, 180, 90, 180], runs);
    }

    [Fact]
    public void Days_override_week_selects_a_different_plan_per_session()
    {
        Assert.Equal(3, RunSession.For(Couch, week: 5, day: 1).RunCount);
        Assert.Equal(2, RunSession.For(Couch, week: 5, day: 2).RunCount);

        var twentyMinuteRun = RunSession.For(Couch, week: 5, day: 3);
        Assert.Equal(1, twentyMinuteRun.RunCount);
        Assert.Equal(1200, twentyMinuteRun.RunSeconds);
        Assert.Equal([1, 2, 3], RunSession.DaysOf(Couch, 5));
    }

    [Fact]
    public void Halfway_cue_of_the_twenty_minute_run_fires_at_the_declared_second()
    {
        var session = RunSession.For(Couch, week: 5, day: 3);
        var cue = Assert.Single(session.Cues, c => c.Key == "cue.halfway");

        Assert.Equal(900, cue.AtSecond); // 300s warmup + 600s into the run
        Assert.Equal(600, cue.RemainingInSegment);
    }

    [Fact]
    public void Every_segment_of_week_three_announces_its_start()
    {
        var session = RunSession.For(Couch, week: 3, day: 1);
        var starts = session.Cues.Where(c => c.CueKind == RunCueKind.SegmentStart).ToArray();

        Assert.Equal(session.Segments.Count, starts.Length);
        Assert.Equal(session.Segments.Select((_, i) => session.SegmentStartSecond(i)), starts.Select(c => c.AtSecond));
    }

    [Fact]
    public void Cue_text_speaks_english_by_default_and_dutch_when_asked()
    {
        var session = RunSession.For(Couch, week: 3, day: 1);
        var warmupEnding = session.Cues.First(c => c.Key == "cue.warmupEnding");
        var runStart = session.Cues.First(c => c.Key == "cue.startRun");

        Assert.Equal("Almost done with the warm-up. Get ready for your first run.", RunCueText.Speak(warmupEnding));
        Assert.Equal("Run, 1 minute 30.", RunCueText.Speak(runStart));
        Assert.Equal("Rennen, 1 minuut 30.", RunCueText.Speak(runStart, "nl"));
    }

    [Fact]
    public void Ending_soon_text_names_what_comes_next()
    {
        var session = Short();
        var beforeRun = session.Cues.First(c => c.CueKind == RunCueKind.EndingSoon);

        Assert.Equal("10 seconds to go. Get ready to run.", RunCueText.Speak(beforeRun));
    }
}
