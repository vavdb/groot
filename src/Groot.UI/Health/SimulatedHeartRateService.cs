using System.Diagnostics;

namespace Groot.UI.Health;

/// <summary>
/// A heart rate monitor that is not there. Produces a plausible run: a settling minute, then a
/// rate that climbs while running and falls while walking, from one device and optionally two.
/// <para>
/// This exists because the Android emulator has no Bluetooth adapter, so the one thing that
/// cannot be checked before going outside is everything downstream of a reading arriving: the
/// trace, the zone colours, the chips, the route's colouring, and what reaches the database. It
/// is a test seam, not a feature. Nothing selects it at runtime — a build has to ask for it with
/// <c>-p:SimulateSensors=true</c>, so it cannot reach a release by accident.
/// </para>
/// <para>
/// It says nothing about whether the real Bluetooth client works. That still needs a watch.
/// </para>
/// </summary>
public sealed class SimulatedHeartRateService : IHeartRateService
{
    /// <summary>How long the fake monitor takes to be found, so the searching state is visible.</summary>
    private static readonly TimeSpan TimeToConnect = TimeSpan.FromSeconds(6);

    /// <summary>When the second monitor joins, so the dashed line arriving is visible too.</summary>
    private static readonly TimeSpan SecondJoinsAt = TimeSpan.FromSeconds(20);

    private readonly Stopwatch _since = new();

    /// <inheritdoc />
    public bool IsSupported => true;

    /// <inheritdoc />
    public SensorState State =>
        !_since.IsRunning ? SensorState.Off
        : _since.Elapsed < TimeToConnect ? SensorState.Searching
        : SensorState.Live;

    /// <inheritdoc />
    public string? Trouble => _since.IsRunning && _since.Elapsed < TimeToConnect
        ? "Simulated monitor. This build was made with SimulateSensors, so no radio is in use."
        : null;

    /// <inheritdoc />
    public IReadOnlyList<HeartRateDevice> Devices
    {
        get
        {
            if (!_since.IsRunning) return [];

            var seconds = _since.Elapsed.TotalSeconds;
            if (seconds < TimeToConnect.TotalSeconds) return [];

            var devices = new List<HeartRateDevice>
            {
                new("simulated-1", "Test strap", SensorState.Live, Bpm(seconds, offset: 0)),
            };

            if (seconds >= SecondJoinsAt.TotalSeconds)
                devices.Add(new HeartRateDevice("simulated-2", "Test watch", SensorState.Live, Bpm(seconds, offset: 5)));

            return devices;
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_since.IsRunning) _since.Restart();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        _since.Reset();
        return Task.CompletedTask;
    }

    /// <summary>
    /// A rate that sweeps the whole range over about three minutes, so every zone the trace can
    /// draw appears without anyone having to run for half an hour to see the top one.
    /// </summary>
    private static int Bpm(double seconds, int offset) =>
        (int)(122 + offset + 56 * Math.Sin(seconds / 29) + 4 * Math.Sin(seconds / 3.1));
}
