namespace Groot.Core.Health;

/// <summary>How hard a reading is, as a share of the highest heart rate the runner can reach.</summary>
public enum HeartRateZone
{
    /// <summary>Under 60% of maximum. Walking, or the first minute of a warmup.</summary>
    Easy,

    /// <summary>60% to 70%. A pace that can be held for an hour.</summary>
    Steady,

    /// <summary>70% to 80%. The middle of a 0→5K run block.</summary>
    Moderate,

    /// <summary>80% to 90%. Breathing hard, talking in short pieces.</summary>
    Hard,

    /// <summary>90% and up. Minutes, not tens of minutes.</summary>
    Maximum,
}

/// <summary>Turns a reading into a zone. The only place the five thresholds are written down.</summary>
public static class HeartRateZones
{
    /// <summary>
    /// The maximum used when the runner has not set one. 190 is the 220-minus-age estimate for
    /// a thirty year old, and the estimate is worth about 10 bpm either way, so a real number
    /// from settings beats it whenever there is one.
    /// </summary>
    public const int DefaultMaximumBpm = 190;

    /// <summary>The zone a reading falls in, against a maximum heart rate.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The maximum is not a heart rate.</exception>
    public static HeartRateZone Of(int bpm, int maximumBpm = DefaultMaximumBpm)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBpm, HeartRateSample.MinimumPlausibleBpm);

        var share = bpm / (double)maximumBpm;
        return share switch
        {
            < 0.60 => HeartRateZone.Easy,
            < 0.70 => HeartRateZone.Steady,
            < 0.80 => HeartRateZone.Moderate,
            < 0.90 => HeartRateZone.Hard,
            _ => HeartRateZone.Maximum,
        };
    }
}
