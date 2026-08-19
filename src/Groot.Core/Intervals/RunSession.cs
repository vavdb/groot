using Groot.Core.Programs;

namespace Groot.Core.Intervals;

/// <summary>Which session of which program this is — everything the screen puts in its header.</summary>
public sealed record RunSessionId(string ProgramId, string ProgramName, int Week, int Day);

/// <summary>Immutable snapshot of a run session, one level above <see cref="IntervalPosition"/>.</summary>
public sealed record RunProgress(
    int ElapsedTotal,
    int TotalSeconds,
    int RemainingTotal,
    int SegmentIndex,
    int SegmentCount,
    Segment Segment,
    int ElapsedInSegment,
    int RemainingInSegment,
    Segment? NextSegment,
    int RunOrdinal,
    int RunCount,
    bool Finished);

/// <summary>
/// Drives one interval session: picks the plan for a week/day, wraps <see cref="IntervalEngine"/>,
/// and precomputes the full cue schedule (declared cues plus the automatic segment-start and
/// ending-soon cues from the program's <see cref="CueDefaults"/>). Pure and deterministic —
/// the platform owns the clock and the speaker.
/// </summary>
public sealed class RunSession
{
    private readonly IntervalEngine _engine;
    private readonly int[] _segmentStarts;
    private readonly RunCue[] _cues;

    public RunSession(IReadOnlyList<Segment> segments, CueDefaults? cueDefaults = null, RunSessionId? id = null)
    {
        if (segments.Count == 0) throw new ArgumentException("Session needs at least one segment.", nameof(segments));
        if (segments.Any(s => s.Seconds <= 0)) throw new ArgumentException("Every segment needs a positive duration.", nameof(segments));

        // Defensive copy: the caller's list (and each segment's cue list) must not be mutable
        // out from under us after construction — copy both levels and hide the arrays behind
        // read-only wrappers so a cast back to the concrete array can't recover write access.
        var snapshot = segments.Select(s => s with { Cues = s.Cues?.ToArray() }).ToArray();

        Segments = Array.AsReadOnly(snapshot);
        CueDefaults = cueDefaults ?? new CueDefaults();
        Id = id;
        _engine = new IntervalEngine(snapshot);
        _segmentStarts = SegmentStarts(snapshot);
        TotalSeconds = _segmentStarts[^1] + snapshot[^1].Seconds;
        RunCount = snapshot.Count(s => s.Kind == SegmentKind.Run);
        RunSeconds = snapshot.Where(s => s.Kind == SegmentKind.Run).Sum(s => s.Seconds);
        _cues = BuildCues();
        Cues = Array.AsReadOnly(_cues);
    }

    public static RunSession For(IntervalProgram program, int week, int day) =>
        new(program.Week(week).PlanFor(day),
            program.CueDefaults,
            new RunSessionId(program.Id, program.Name, week, day));

    /// <summary>Session numbers of a week, so a picker never has to guess.</summary>
    public static IReadOnlyList<int> DaysOf(IntervalProgram program, int week) => program.Week(week).DayNumbers;

    public RunSessionId? Id { get; }

    public IReadOnlyList<Segment> Segments { get; }

    public CueDefaults CueDefaults { get; }

    public int TotalSeconds { get; }

    public int RunCount { get; }

    public int RunSeconds { get; }

    /// <summary>The whole cue schedule, ordered by second. Handy for tests and for a session preview.</summary>
    public IReadOnlyList<RunCue> Cues { get; }

    public int SegmentStartSecond(int segmentIndex) => _segmentStarts[segmentIndex];

    public RunProgress ProgressAt(int elapsedSeconds)
    {
        var position = _engine.PositionAt(elapsedSeconds);
        var next = position.SegmentIndex + 1 < Segments.Count ? Segments[position.SegmentIndex + 1] : null;

        return new RunProgress(
            position.ElapsedTotal,
            TotalSeconds,
            position.RemainingTotal,
            position.SegmentIndex,
            Segments.Count,
            position.Segment,
            position.ElapsedInSegment,
            position.RemainingInSegment,
            position.Finished ? null : next,
            RunOrdinalOf(position.SegmentIndex),
            RunCount,
            position.Finished);
    }

    /// <summary>
    /// Cues due in the window (<paramref name="fromExclusive"/>, <paramref name="toInclusive"/>].
    /// Start a session with fromExclusive = -1 so the cue at second 0 fires; after a clock jump
    /// pass the whole skipped window and every cue in it comes back exactly once.
    /// </summary>
    public IReadOnlyList<RunCue> CuesBetween(int fromExclusive, int toInclusive) =>
        _cues.Where(c => c.AtSecond > fromExclusive && c.AtSecond <= toInclusive).ToArray();

    /// <summary>Elapsed second to jump to when the runner skips the current segment.</summary>
    public int SkipTargetFrom(int elapsedSeconds)
    {
        var index = ProgressAt(elapsedSeconds).SegmentIndex;
        return index + 1 < Segments.Count ? _segmentStarts[index + 1] : TotalSeconds;
    }

    /// <summary>1-based position of a segment among the run segments; 0 when it is a walk.</summary>
    public int RunOrdinalOf(int segmentIndex) =>
        Segments[segmentIndex].Kind == SegmentKind.Run
            ? Segments.Take(segmentIndex + 1).Count(s => s.Kind == SegmentKind.Run)
            : 0;

    private static int[] SegmentStarts(IReadOnlyList<Segment> segments)
    {
        var starts = new int[segments.Count];
        var running = 0;
        for (var i = 0; i < segments.Count; i++)
        {
            starts[i] = running;
            running += segments[i].Seconds;
        }
        return starts;
    }

    private RunCue[] BuildCues()
    {
        var cues = new List<RunCue>();
        // int.MinValue has no positive counterpart (Math.Abs would overflow) — clamp it away first.
        var endingSoonLead = Math.Abs(Math.Max(CueDefaults.EndingSoonCueAtSeconds, int.MinValue + 1));

        for (var i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            var start = _segmentStarts[i];
            var end = start + segment.Seconds;

            if (CueDefaults.SegmentStartCue)
                cues.Add(Cue(StartKeyOf(segment.Kind), RunCueKind.SegmentStart, start, i));

            foreach (var declared in segment.Cues ?? [])
            {
                var at = declared.At >= 0 ? start + declared.At : end + declared.At;
                if (at < start || at > end) continue;
                cues.Add(Cue(declared.Key, RunCueKind.Declared, at, i));
            }

            if (endingSoonLead > 0 && segment.Seconds > endingSoonLead)
                cues.Add(Cue("cue.endingSoon", RunCueKind.EndingSoon, end - endingSoonLead, i));
        }

        cues.Add(Cue("cue.finished", RunCueKind.Finished, TotalSeconds, Segments.Count - 1));

        return cues
            .OrderBy(c => c.AtSecond)
            .ThenBy(c => c.SegmentIndex)
            .ThenBy(c => (int)c.CueKind)
            .ToArray();
    }

    private RunCue Cue(string key, RunCueKind cueKind, int at, int segmentIndex)
    {
        var segment = Segments[segmentIndex];
        var next = segmentIndex + 1 < Segments.Count ? Segments[segmentIndex + 1] : null;
        var lookFrom = cueKind is RunCueKind.EndingSoon or RunCueKind.Finished ? segmentIndex + 1 : segmentIndex;

        return new RunCue(
            key,
            cueKind,
            at,
            segmentIndex,
            segment.Kind,
            segment.Label,
            segment.Seconds,
            next?.Kind,
            next?.Seconds,
            NextRunOrdinalFrom(lookFrom),
            _segmentStarts[segmentIndex] + segment.Seconds - at,
            TotalSeconds - at);
    }

    private int NextRunOrdinalFrom(int segmentIndex)
    {
        for (var i = Math.Max(0, segmentIndex); i < Segments.Count; i++)
            if (Segments[i].Kind == SegmentKind.Run)
                return RunOrdinalOf(i);
        return 0;
    }

    private static string StartKeyOf(SegmentKind kind) =>
        kind == SegmentKind.Run ? "cue.startRun" : "cue.startWalk";
}
