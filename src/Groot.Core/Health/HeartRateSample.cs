namespace Groot.Core.Health;

/// <summary>
/// One heart rate reading, stamped with how far into the session it arrived. Elapsed seconds
/// rather than a wall clock keeps the engine deterministic: the screen already owns a monotonic
/// clock, and a sample that crosses a pause carries the paused-out elapsed value, not real time.
/// </summary>
/// <param name="ElapsedSeconds">Seconds into the session, from the same clock the run screen shows.</param>
/// <param name="Bpm">Beats per minute, as the device reported it.</param>
public sealed record HeartRateSample(int ElapsedSeconds, int Bpm)
{
    /// <summary>Below this a reading is a dropped connection or a sensor that lost the skin, not a heart rate.</summary>
    public const int MinimumPlausibleBpm = 25;

    /// <summary>Above this a reading is sensor noise. Cadence lock on a wrist optical sensor lands here.</summary>
    public const int MaximumPlausibleBpm = 250;

    /// <summary>Whether this reading is worth plotting. Implausible values are dropped, never clamped.</summary>
    public bool IsPlausible =>
        Bpm is >= MinimumPlausibleBpm and <= MaximumPlausibleBpm && ElapsedSeconds >= 0;
}
