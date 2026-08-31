namespace Groot.Core.Health;

/// <summary>
/// What a trace looks like right now: one bpm per bucket for the part of the session that has
/// happened, nothing for the part that has not, and the readings a screen puts beside the graph.
/// A view is a snapshot; the screen re-reads it every tick rather than holding on to one.
/// </summary>
/// <param name="Buckets">One entry per bucket across the whole session. Null where no reading landed.</param>
/// <param name="FilledBuckets">How many buckets from the left have a reading. The rest are the empty track.</param>
/// <param name="Axis">The vertical range these buckets are drawn against.</param>
/// <param name="Current">The most recent reading, or null before the first one arrives.</param>
/// <param name="Minimum">Lowest reading so far, or null before the first one.</param>
/// <param name="Maximum">Highest reading so far, or null before the first one.</param>
/// <param name="Average">Mean of every reading so far, or null before the first one.</param>
/// <param name="SampleCount">How many readings went in. Zero means the trace is empty.</param>
public sealed record HeartRateTraceView(
    IReadOnlyList<int?> Buckets,
    int FilledBuckets,
    HeartRateAxis Axis,
    int? Current,
    int? Minimum,
    int? Maximum,
    int? Average,
    int SampleCount)
{
    /// <summary>Whether anything has been recorded yet.</summary>
    public bool IsEmpty => SampleCount == 0;

    /// <summary>A trace with no readings, drawn as an empty track at the default range.</summary>
    public static HeartRateTraceView Empty(int buckets) =>
        new(new int?[Math.Max(1, buckets)], 0, HeartRateAxis.Default, null, null, null, null, 0);
}
