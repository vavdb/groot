using Groot.UI.Health;

namespace Groot.App.Platforms.Android;

/// <summary>
/// Where the phone is, through MAUI's own geolocation. Foreground listening: the fixes stop when
/// the app goes to the background, which is why the run screen keeps the display awake while a
/// session is running. A route that survives a locked phone needs a foreground service, which
/// this does not yet have.
/// </summary>
public sealed class AndroidLocationService : ILocationService
{
    // A second is what a running pace needs: at 10 km/h that is a fix every three metres, well
    // clear of the two-metre filter the route track applies.
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private long _sequence;
    private bool _listening;

    /// <inheritdoc />
    public bool IsSupported => true;

    /// <inheritdoc />
    public SensorState State { get; private set; } = SensorState.Off;

    /// <inheritdoc />
    public LocationFix? Latest { get; private set; }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listening) return;

        if (!await RequestPermissionAsync())
        {
            State = SensorState.Denied;
            return;
        }

        State = SensorState.Searching;

        try
        {
            Geolocation.Default.LocationChanged += OnLocationChanged;
            Geolocation.Default.ListeningFailed += OnListeningFailed;

            var started = await Geolocation.Default.StartListeningForegroundAsync(
                new GeolocationListeningRequest(GeolocationAccuracy.Best, Interval));

            _listening = started;
            if (!started) State = SensorState.Off;
        }
        catch (Exception)
        {
            // A phone with location switched off system-wide throws rather than refusing.
            State = SensorState.Off;
            _listening = false;
        }
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.ListeningFailed -= OnListeningFailed;

        try
        {
            Geolocation.Default.StopListeningForeground();
        }
        catch (Exception)
        {
            // Not listening, or already torn down. Either way there is nothing to stop.
        }

        _listening = false;
        State = SensorState.Off;
        Latest = null;
        return Task.CompletedTask;
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var location = e.Location;

        Latest = new LocationFix(
            location.Latitude,
            location.Longitude,
            // A device that will not say how good the fix is has not really given one, so this
            // sends it through as far worse than the route track will accept.
            location.Accuracy ?? 9_999,
            ++_sequence);

        State = SensorState.Live;
    }

    private void OnListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        // The fix comes and goes under trees and between buildings. Lost, not off: the run
        // screen says so on its chip and the route picks up again where it resumes.
        State = SensorState.Lost;
        _listening = false;
    }

    private static async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            return status == PermissionStatus.Granted;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
