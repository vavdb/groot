namespace Groot.Core.Health;

/// <summary>
/// Collects position reports for one session and hands out a <see cref="RouteTraceView"/> to draw.
/// Keeps every fix it accepts, which is a few thousand for a long run and small enough to hold.
/// <para>
/// Deterministic given the order fixes go in; nothing here reads a clock. Two filters do most of
/// the work: a fix the device is not confident about is dropped outright, and one that has not
/// moved far enough to be a step is dropped as jitter. A phone standing still reports a position
/// that wanders several metres a second, and without the second filter a warmup spent waiting at
/// a crossing adds a few hundred metres to the distance.
/// </para>
/// </summary>
public sealed class RouteTrack
{
    /// <summary>
    /// A fix closer than this to the one before it is treated as the same place. Below a stride,
    /// so it removes standing-still wander without swallowing slow walking.
    /// </summary>
    public const double MinimumMoveMetres = 2;

    /// <summary>
    /// A silence longer than this counts as having lost the fix. The straight line across it is
    /// not where the runner went, so the screen is told to draw that join differently.
    /// </summary>
    public const int GapSeconds = 20;

    private readonly List<RouteFix> _fixes = [];
    private readonly List<bool> _gapBefore = [];
    private readonly List<double> _distanceAt = [];

    /// <summary>The fixes that were kept, in order.</summary>
    public IReadOnlyList<RouteFix> Fixes => _fixes;

    /// <summary>Ground covered so far, summed between consecutive kept fixes.</summary>
    public double DistanceMetres { get; private set; }

    /// <summary>The most recent kept fix, or null before the first one.</summary>
    public RouteFix? Last => _fixes.Count == 0 ? null : _fixes[^1];

    /// <summary>
    /// Records one fix. Returns whether it was kept: an unusable fix, one that arrives out of
    /// order, and one that has not moved far enough are all dropped.
    /// </summary>
    public bool Add(RouteFix fix)
    {
        if (!fix.IsUsable) return false;

        if (Last is not { } previous)
        {
            _fixes.Add(fix);
            _gapBefore.Add(false);
            _distanceAt.Add(0);
            return true;
        }

        if (fix.ElapsedSeconds < previous.ElapsedSeconds) return false;

        var moved = Geo.DistanceMetres(previous.Latitude, previous.Longitude, fix.Latitude, fix.Longitude);
        if (moved < MinimumMoveMetres) return false;

        DistanceMetres += moved;
        _fixes.Add(fix);
        _gapBefore.Add(fix.ElapsedSeconds - previous.ElapsedSeconds > GapSeconds);
        _distanceAt.Add(DistanceMetres);
        return true;
    }

    /// <summary>Forgets every fix. The run screen calls this when a session restarts.</summary>
    public void Clear()
    {
        _fixes.Clear();
        _gapBefore.Clear();
        _distanceAt.Clear();
        DistanceMetres = 0;
    }

    /// <summary>
    /// The route flattened onto a unit square, along with the shape of the ground it covers.
    /// <para>
    /// Equirectangular: longitude is scaled by the cosine of the middle latitude so a degree east
    /// and a degree north come out the same size on the ground. Over the few kilometres a run
    /// covers this is accurate to well under a pixel, and unlike a real projection it needs no
    /// tables. The unit square is filled on the longer side; the shorter one is centred in it.
    /// </para>
    /// </summary>
    public RouteTraceView View()
    {
        if (_fixes.Count == 0) return RouteTraceView.Empty;

        var minLat = double.MaxValue;
        var maxLat = double.MinValue;
        var minLon = double.MaxValue;
        var maxLon = double.MinValue;

        foreach (var fix in _fixes)
        {
            minLat = Math.Min(minLat, fix.Latitude);
            maxLat = Math.Max(maxLat, fix.Latitude);
            minLon = Math.Min(minLon, fix.Longitude);
            maxLon = Math.Max(maxLon, fix.Longitude);
        }

        var lonScale = Math.Cos((minLat + maxLat) / 2 * Math.PI / 180);
        var groundWidth = (maxLon - minLon) * lonScale;
        var groundHeight = maxLat - minLat;

        // A route that has not moved yet, or one that is a straight line north: give the flat
        // side a nominal extent so the division below has something to divide by and the whole
        // thing lands on the centre line rather than at an edge.
        var span = Math.Max(Math.Max(groundWidth, groundHeight), double.Epsilon);
        var aspect = groundHeight <= 0 || groundWidth <= 0 ? 1 : groundWidth / groundHeight;

        var offsetX = (span - groundWidth) / 2;
        var offsetY = (span - groundHeight) / 2;

        var points = new List<RoutePlot>(_fixes.Count);
        for (var i = 0; i < _fixes.Count; i++)
        {
            var fix = _fixes[i];
            points.Add(new RoutePlot(
                (offsetX + (fix.Longitude - minLon) * lonScale) / span,
                // Latitude climbs north and a screen's y climbs south, so this one flips.
                (offsetY + (maxLat - fix.Latitude)) / span,
                fix.ElapsedSeconds,
                _distanceAt[i],
                fix.Bpm,
                _gapBefore[i]));
        }

        return new RouteTraceView(points, DistanceMetres, aspect);
    }
}
