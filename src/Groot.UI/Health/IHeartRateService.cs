namespace Groot.UI.Health;

/// <summary>One heart rate monitor as the screen sees it.</summary>
/// <param name="Id">Stable per device, and the key its readings are stored under.</param>
/// <param name="Name">What to call it on screen: "Amazfit", "Fitbit Air".</param>
/// <param name="State">Where the connection is.</param>
/// <param name="Bpm">The most recent reading, or null if none has arrived.</param>
public sealed record HeartRateDevice(string Id, string Name, SensorState State, int? Bpm);

/// <summary>
/// Every heart rate monitor the app is listening to. One service rather than one object per
/// device, because a phone is the central in a Bluetooth conversation and can hold several
/// monitors at once: the per-device limit in the vendors' documentation is on how many phones
/// may connect to one watch, not the other way round.
/// <para>
/// The screen polls this on its own tick instead of subscribing to an event. A monitor reports
/// about once a second and the run screen already wakes four times a second, so polling costs
/// nothing and keeps every reading on the render thread, with the screen's own clock deciding
/// where in the session it lands.
/// </para>
/// </summary>
public interface IHeartRateService
{
    /// <summary>Whether this head can talk to heart rate monitors at all. False on the web.</summary>
    bool IsSupported { get; }

    /// <summary>The service as a whole: the best state of any device it is holding.</summary>
    SensorState State { get; }

    /// <summary>Every monitor being listened to, connected or not.</summary>
    IReadOnlyList<HeartRateDevice> Devices { get; }

    /// <summary>
    /// Starts listening. Safe to call again while already listening. Never throws for a refused
    /// permission or a radio that is switched off: those surface as a <see cref="SensorState"/>.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops listening and drops every connection.</summary>
    Task StopAsync();
}

/// <summary>The heart rate service for a head that has no Bluetooth, such as the web one.</summary>
public sealed class NoHeartRateService : IHeartRateService
{
    public bool IsSupported => false;

    public SensorState State => SensorState.Off;

    public IReadOnlyList<HeartRateDevice> Devices => [];

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;
}
