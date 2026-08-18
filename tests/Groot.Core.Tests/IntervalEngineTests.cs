using Groot.Core.Intervals;

namespace Groot.Core.Tests;

public class IntervalEngineTests
{
    // 0->5K week 3 shape, shortened: warmup 300, run 90, walk 90, cooldown 300
    private static IntervalEngine Engine() => new(
    [
        new Segment(SegmentKind.Walk, 300, "warmup", [new CuePoint(-15, "cue.warmupEnding")]),
        new Segment(SegmentKind.Run, 90),
        new Segment(SegmentKind.Walk, 90),
        new Segment(SegmentKind.Walk, 300, "cooldown"),
    ]);

    [Fact]
    public void Position_inside_first_segment()
    {
        var pos = Engine().PositionAt(120);
        Assert.Equal(0, pos.SegmentIndex);
        Assert.Equal(120, pos.ElapsedInSegment);
        Assert.Equal(180, pos.RemainingInSegment);
        Assert.False(pos.Finished);
    }

    [Fact]
    public void Position_lands_in_run_segment_after_warmup()
    {
        var pos = Engine().PositionAt(310);
        Assert.Equal(1, pos.SegmentIndex);
        Assert.Equal(SegmentKind.Run, pos.Segment.Kind);
        Assert.Equal(10, pos.ElapsedInSegment);
    }

    [Fact]
    public void Session_finishes_at_total_duration()
    {
        var pos = Engine().PositionAt(780);
        Assert.True(pos.Finished);
        Assert.Equal(0, pos.RemainingTotal);
    }

    [Fact]
    public void Negative_cue_fires_before_segment_end()
    {
        // warmup ends at 300; cue at -15 => absolute second 285
        var cues = Engine().CuesBetween(280, 290);
        var cue = Assert.Single(cues);
        Assert.Equal("cue.warmupEnding", cue.Key);
        Assert.Equal(SegmentKind.Run, cue.NextKind);
    }

    [Fact]
    public void Cue_does_not_fire_twice()
    {
        var engine = Engine();
        Assert.Single(engine.CuesBetween(280, 290));
        Assert.Empty(engine.CuesBetween(290, 300));
    }

    [Fact]
    public void Clock_jump_still_collects_cue()
    {
        // app resumed after 30s pause: window (270, 300] catches the -15 cue exactly once
        var cues = Engine().CuesBetween(270, 300);
        Assert.Single(cues);
    }
}
