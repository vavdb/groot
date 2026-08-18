using Groot.Core.Intervals;

namespace Groot.Core.Programs;

public enum ProgramType { SetsReps, Intervals }

/// <summary>Every program in the catalog, whatever its type, exposes this much.</summary>
public sealed record ProgramSummary(string Id, string Name, int Version, ProgramType Type);

/// <summary>
/// Automatic cues the engine adds on top of the cues a segment declares:
/// one at every segment start, one shortly before every segment ends.
/// </summary>
public sealed record CueDefaults(bool SegmentStartCue = true, int EndingSoonCueAtSeconds = -10);

/// <summary>One session of an interval week. Weeks that vary per session list these explicitly.</summary>
public sealed record IntervalDay(int Day, IReadOnlyList<Segment> Plan);

/// <summary>
/// A week of an interval program: either one uniform <paramref name="Plan"/> repeated for every
/// session, or per-day plans (0→5K weeks 5 and 6). Exactly one of the two is set.
/// </summary>
public sealed record IntervalWeek(
    int Week,
    int SessionsPerWeek,
    IReadOnlyList<Segment>? Plan,
    IReadOnlyList<IntervalDay>? Days)
{
    /// <summary>Session numbers available in this week, 1-based and ascending.</summary>
    public IReadOnlyList<int> DayNumbers =>
        Days is { } days
            ? days.Select(d => d.Day).OrderBy(d => d).ToArray()
            : Enumerable.Range(1, SessionsPerWeek).ToArray();

    public IReadOnlyList<Segment> PlanFor(int day)
    {
        if (Days is { } days)
            return days.FirstOrDefault(d => d.Day == day)?.Plan
                ?? throw new ArgumentOutOfRangeException(nameof(day), day, $"Week {Week} has no day {day}.");

        if (day < 1 || day > SessionsPerWeek)
            throw new ArgumentOutOfRangeException(nameof(day), day, $"Week {Week} has {SessionsPerWeek} sessions.");

        return Plan ?? throw new InvalidOperationException($"Week {Week} has neither a plan nor days.");
    }
}

public sealed record IntervalProgram(
    string Id,
    string Name,
    int Version,
    CueDefaults CueDefaults,
    IReadOnlyList<IntervalWeek> Weeks)
{
    public ProgramSummary Summary => new(Id, Name, Version, ProgramType.Intervals);

    public IReadOnlyList<int> WeekNumbers => Weeks.Select(w => w.Week).OrderBy(w => w).ToArray();

    public IntervalWeek Week(int week) =>
        Weeks.FirstOrDefault(w => w.Week == week)
        ?? throw new ArgumentOutOfRangeException(nameof(week), week, $"Program {Id} has no week {week}.");

    /// <summary>Total sessions in the program — the denominator of "7 of 27 sessions".</summary>
    public int TotalSessions => Weeks.Sum(w => w.DayNumbers.Count);
}
