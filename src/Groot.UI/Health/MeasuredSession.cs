using Groot.Core.Health;
using Groot.Core.Intervals;

namespace Groot.UI.Health;

/// <summary>
/// A finished run and what was measured during it, for a head to store. The run screen builds
/// this; where it goes is the head's business, and the screen never learns.
/// </summary>
/// <param name="Session">Which session it was: program, week and day.</param>
/// <param name="DurationSeconds">How long the session was.</param>
/// <param name="Measured">Heart rate and route.</param>
public sealed record MeasuredRun(RunSessionId? Session, int DurationSeconds, MeasuredSession Measured);

/// <summary>
/// What a session measured, handed to the owner when it finishes. The run screen produces this
/// and knows nothing about where it goes; a head passes it to the store, and later to whatever
/// exports it.
/// </summary>
/// <param name="HeartRate">Every reading, per monitor, keyed by the monitor's device id.</param>
/// <param name="Route">Every position fix that was kept, in order.</param>
/// <param name="DistanceMetres">Ground covered, as the route track measured it.</param>
public sealed record MeasuredSession(
    IReadOnlyDictionary<string, IReadOnlyList<HeartRateSample>> HeartRate,
    IReadOnlyList<RouteFix> Route,
    double DistanceMetres)
{
    /// <summary>Whether anything at all was measured. A run with no watch and no fix is this.</summary>
    public bool IsEmpty => Route.Count == 0 && HeartRate.All(entry => entry.Value.Count == 0);

    /// <summary>A session that measured nothing.</summary>
    public static MeasuredSession Empty { get; } =
        new(new Dictionary<string, IReadOnlyList<HeartRateSample>>(), [], 0);
}
