namespace Groot.UI.Health;

/// <summary>One position report, before the session's clock has been stamped on it.</summary>
/// <param name="Latitude">Degrees north.</param>
/// <param name="Longitude">Degrees east.</param>
/// <param name="AccuracyMetres">The radius the device claims the position is good to.</param>
/// <param name="Sequence">
/// Counts up with every fix the device produces. The screen polls faster than the device
/// reports, and this is how it tells a new fix from the same one seen again.
/// </param>
public sealed record LocationFix(double Latitude, double Longitude, double AccuracyMetres, long Sequence);

/// <summary>
/// Where the phone is, while a session is running. Polled by the screen on its own tick, for the
/// same reasons as <see cref="IHeartRateService"/>.
/// </summary>
public interface ILocationService
{
    /// <summary>Whether this head can produce a position at all. False on the web.</summary>
    bool IsSupported { get; }

    /// <summary>Where the fix is.</summary>
    SensorState State { get; }

    /// <summary>The most recent fix, or null before the first one lands.</summary>
    LocationFix? Latest { get; }

    /// <summary>
    /// Starts following the phone. Safe to call again while already following. Never throws for
    /// a refused permission: that surfaces as <see cref="SensorState.Denied"/>.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops following, and lets the radio go back to sleep.</summary>
    Task StopAsync();

    /// <summary>One short sentence about why there is no fix, or null when there is nothing to say.</summary>
    string? Trouble { get; }
}

/// <summary>The location service for a head that cannot produce one, such as the web head.</summary>
public sealed class NoLocationService : ILocationService
{
    public bool IsSupported => false;

    public SensorState State => SensorState.Off;

    public LocationFix? Latest => null;

    public string? Trouble => null;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;
}
