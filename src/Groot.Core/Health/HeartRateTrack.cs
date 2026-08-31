namespace Groot.Core.Health;

/// <summary>
/// Collects heart rate readings for one session from one device and hands out a
/// <see cref="HeartRateTraceView"/> to draw. Fixed memory: readings fold into a bucket as they
/// arrive, so a 30 minute session costs the same as a 90 minute one.
/// <para>
/// Deterministic given the order readings go in. The caller owns the clock; nothing here reads it.
/// A device that reports twice in one bucket is averaged, which is what a bucket means: the
/// heart rate over that slice of the session, not the last value that happened to land in it.
/// </para>
/// </summary>
public sealed class HeartRateTrack
{
    /// <summary>Buckets across the session. 40 is one bar per 45 seconds in a 30 minute session.</summary>
    public const int DefaultBuckets = 40;

    private readonly int _totalSeconds;
    private readonly long[] _sums;
    private readonly int[] _counts;

    private int _samples;
    private int _total;
    private int _current;
    private int _minimum = int.MaxValue;
    private int _maximum = int.MinValue;
    private int _highestBucket = -1;

    /// <summary>
    /// A track for a session of <paramref name="totalSeconds"/>, drawn across
    /// <paramref name="buckets"/> slices.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The session has no length, or there are no buckets.</exception>
    public HeartRateTrack(int totalSeconds, int buckets = DefaultBuckets)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(totalSeconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(buckets, 1);

        _totalSeconds = totalSeconds;
        Buckets = buckets;
        _sums = new long[buckets];
        _counts = new int[buckets];
    }

    /// <summary>How many slices the session is drawn across.</summary>
    public int Buckets { get; }

    /// <summary>How many plausible readings have been recorded.</summary>
    public int SampleCount => _samples;

    /// <summary>The most recent plausible reading, or null before the first one.</summary>
    public int? Current => _samples == 0 ? null : _current;

    /// <summary>
    /// Records one reading. Implausible values and readings past the end of the session are
    /// dropped, so a watch that keeps broadcasting after the last segment cannot stretch the
    /// trace past the finish. Returns whether the reading was kept.
    /// </summary>
    public bool Add(HeartRateSample sample)
    {
        if (!sample.IsPlausible || sample.ElapsedSeconds > _totalSeconds) return false;

        var bucket = BucketOf(sample.ElapsedSeconds);
        _sums[bucket] += sample.Bpm;
        _counts[bucket]++;
        if (bucket > _highestBucket) _highestBucket = bucket;

        _samples++;
        _total += sample.Bpm;
        _current = sample.Bpm;
        if (sample.Bpm < _minimum) _minimum = sample.Bpm;
        if (sample.Bpm > _maximum) _maximum = sample.Bpm;
        return true;
    }

    /// <summary>Records a reading at an elapsed time. A convenience over <see cref="Add(HeartRateSample)"/>.</summary>
    public bool Add(int elapsedSeconds, int bpm) => Add(new HeartRateSample(elapsedSeconds, bpm));

    /// <summary>Forgets every reading. The run screen calls this when a session restarts.</summary>
    public void Clear()
    {
        Array.Clear(_sums);
        Array.Clear(_counts);
        _samples = 0;
        _total = 0;
        _current = 0;
        _minimum = int.MaxValue;
        _maximum = int.MinValue;
        _highestBucket = -1;
    }

    /// <summary>
    /// A snapshot to draw. Buckets between two readings are filled from the reading before them,
    /// so a device that reports every five seconds and a device that reports every thirty both
    /// produce an unbroken line up to where the session has got to.
    /// </summary>
    public HeartRateTraceView View()
    {
        if (_samples == 0) return HeartRateTraceView.Empty(Buckets);

        var values = new int?[Buckets];
        int? carried = null;

        for (var i = 0; i <= _highestBucket; i++)
        {
            if (_counts[i] > 0) carried = (int)Math.Round(_sums[i] / (double)_counts[i]);
            values[i] = carried; // null only for buckets before the very first reading
        }

        return new HeartRateTraceView(
            values,
            _highestBucket + 1,
            HeartRateAxis.For(_minimum, _maximum),
            _current,
            _minimum,
            _maximum,
            (int)Math.Round(_total / (double)_samples),
            _samples);
    }

    private int BucketOf(int elapsedSeconds) =>
        Math.Clamp(elapsedSeconds * Buckets / _totalSeconds, 0, Buckets - 1);
}
