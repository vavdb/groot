using Groot.Core.Health;
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
    public string? Trouble { get; private set; }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent. Starting a session while the fix is already good must not tear it down and
        // go back to searching, which is what the run screen would otherwise do every time Start
        // is pressed. A real retry goes through Stop first; the run screen's chip does that.
        if (_listening) return;

        if (!await RequestPermissionAsync())
        {
            State = SensorState.Denied;
            Trouble = "Groot needs the Location permission to record where you ran.";
            return;
        }

        State = SensorState.Searching;
        Trouble = null;

        try
        {
            Geolocation.Default.LocationChanged += OnLocationChanged;
            Geolocation.Default.ListeningFailed += OnListeningFailed;

            var started = await Geolocation.Default.StartListeningForegroundAsync(
                new GeolocationListeningRequest(GeolocationAccuracy.Best, Interval));

            _listening = started;

            if (!started)
            {
                State = SensorState.Off;
                Trouble = "Android would not start location updates.";
            }
        }
        catch (FeatureNotEnabledException)
        {
            State = SensorState.Off;
            _listening = false;
            Trouble = "Location is switched off on this phone.";
        }
        catch (Exception exception)
        {
            State = SensorState.Off;
            _listening = false;
            Trouble = $"Location failed to start: {exception.GetType().Name}.";
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

        // A fix can be real and still too vague to draw. Saying so beats an empty map: a first
        // fix indoors is often 50 m or worse and settles within a minute of being outside.
        Trouble = location.Accuracy is { } accuracy && accuracy > RouteFix.WorstUsableAccuracyMetres
            ? $"The fix is only good to {accuracy:0} m, so nothing is drawn yet."
            : null;
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
