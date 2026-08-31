namespace Groot.Core.Health;

/// <summary>
/// One position report from the device, stamped with how far into the session it arrived.
/// Elapsed seconds rather than a wall clock, for the same reason <see cref="HeartRateSample"/>
/// uses them: the screen owns the clock, and a paused session holds its place.
/// </summary>
/// <param name="ElapsedSeconds">Seconds into the session, from the run screen's clock.</param>
/// <param name="Latitude">Degrees north, -90 to 90.</param>
/// <param name="Longitude">Degrees east, -180 to 180.</param>
/// <param name="AccuracyMetres">The radius the device claims the position is good to.</param>
/// <param name="Bpm">The heart rate at this point, when a monitor is connected.</param>
public sealed record RouteFix(
    int ElapsedSeconds,
    double Latitude,
    double Longitude,
    double AccuracyMetres,
    int? Bpm = null)
{
    /// <summary>
    /// Fixes claiming worse than this are dropped. A phone under a coat between buildings
    /// reports 100 m or more and wanders a street over; drawing that is worse than a gap.
    /// </summary>
    public const double WorstUsableAccuracyMetres = 40;

    /// <summary>Whether this fix is worth keeping.</summary>
    public bool IsUsable =>
        ElapsedSeconds >= 0
        && AccuracyMetres > 0
        && AccuracyMetres <= WorstUsableAccuracyMetres
        && Latitude is >= -90 and <= 90
        && Longitude is >= -180 and <= 180
        && (Latitude != 0 || Longitude != 0); // null island: a device reporting no fix as 0,0
}
