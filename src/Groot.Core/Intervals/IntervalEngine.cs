namespace Groot.Core.Intervals;

public enum SegmentKind { Walk, Run }

/// <summary>A cue point: seconds from segment start (>= 0) or from segment end (negative).</summary>
public sealed record CuePoint(int At, string Key);

public sealed record Segment(SegmentKind Kind, int Seconds, string? Label = null, IReadOnlyList<CuePoint>? Cues = null);

public sealed record CueDue(string Key, int SegmentIndex, SegmentKind NextKind);

/// <summary>Immutable snapshot of where the runner is inside a session.</summary>
public sealed record IntervalPosition(
    int SegmentIndex,
    Segment Segment,
    int ElapsedInSegment,
    int RemainingInSegment,
    int ElapsedTotal,
    int RemainingTotal,
    bool Finished);

/// <summary>
/// Pure segment state machine. Platforms own the clock and audio; this owns the math.
/// Feed it elapsed seconds, get position + due cues back. Deterministic, unit-tested.
/// </summary>
public sealed class IntervalEngine
{
    private readonly IReadOnlyList<Segment> _segments;
    private readonly int _totalSeconds;

    public IntervalEngine(IReadOnlyList<Segment> segments)
    {
        if (segments.Count == 0) throw new ArgumentException("Session needs at least one segment.", nameof(segments));
        _segments = segments;
        _totalSeconds = segments.Sum(s => s.Seconds);
    }

    public IntervalPosition PositionAt(int elapsedTotalSeconds)
    {
        var clamped = Math.Clamp(elapsedTotalSeconds, 0, _totalSeconds);
        var running = 0;
        for (var i = 0; i < _segments.Count; i++)
        {
            var segment = _segments[i];
            if (clamped < running + segment.Seconds || (i == _segments.Count - 1 && clamped <= running + segment.Seconds))
            {
                var inSegment = clamped - running;
                return new IntervalPosition(
                    i, segment, inSegment, segment.Seconds - inSegment,
                    clamped, _totalSeconds - clamped,
                    Finished: clamped >= _totalSeconds);
            }
            running += segment.Seconds;
        }
        var last = _segments[^1];
        return new IntervalPosition(_segments.Count - 1, last, last.Seconds, 0, _totalSeconds, 0, Finished: true);
    }

    /// <summary>
    /// Cues that fire in the window (from, to]. Includes declared cue points; the platform layer
    /// adds the automatic segment-start and -10s cues per the program's cue defaults.
    /// </summary>
    public IReadOnlyList<CueDue> CuesBetween(int fromSecond, int toSecond)
    {
        var due = new List<CueDue>();
        var running = 0;
        for (var i = 0; i < _segments.Count; i++)
        {
            var segment = _segments[i];
            foreach (var cue in segment.Cues ?? Array.Empty<CuePoint>())
            {
                var absolute = cue.At >= 0
                    ? running + cue.At
                    : running + segment.Seconds + cue.At;
                if (absolute > fromSecond && absolute <= toSecond)
                    due.Add(new CueDue(cue.Key, i, NextKindAfter(i)));
            }
            running += segment.Seconds;
        }
        return due;
    }

    private SegmentKind NextKindAfter(int index) =>
        index + 1 < _segments.Count ? _segments[index + 1].Kind : _segments[index].Kind;
}
