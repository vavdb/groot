namespace Groot.Core.Health;

/// <summary>
/// One point of the route, already flattened onto a unit square so a screen can draw it without
/// knowing anything about latitude. X runs 0 (west edge) to 1 (east edge), Y runs 0 (north edge)
/// to 1 (south edge), which is the direction a screen's y axis already goes.
/// </summary>
/// <param name="X">Across the box, 0 to 1.</param>
/// <param name="Y">Down the box, 0 to 1.</param>
/// <param name="ElapsedSeconds">Seconds into the session this point was reached.</param>
/// <param name="Bpm">The heart rate here, when a monitor was connected.</param>
/// <param name="GapBefore">
/// Whether the fix was lost between the previous point and this one. A screen draws the join
/// differently, because the straight line between them is not where the runner went.
/// </param>
public sealed record RoutePlot(double X, double Y, int ElapsedSeconds, int? Bpm, bool GapBefore);

/// <summary>
/// The route so far, ready to draw. A snapshot: the screen re-reads it rather than holding one.
/// </summary>
/// <param name="Points">The route in order, flattened onto a unit square.</param>
/// <param name="DistanceMetres">Ground covered, summed between consecutive kept fixes.</param>
/// <param name="AspectRatio">
/// Width over height of the ground the route covers. A screen scales the unit square by this so
/// a loop that is twice as wide as it is tall draws that way instead of being squared up.
/// </param>
public sealed record RouteView(IReadOnlyList<RoutePlot> Points, double DistanceMetres, double AspectRatio)
{
    /// <summary>Whether there is anything to draw.</summary>
    public bool IsEmpty => Points.Count == 0;

    /// <summary>Distance in kilometres, which is what the screen shows.</summary>
    public double DistanceKm => DistanceMetres / 1000;

    /// <summary>A route with nothing in it yet.</summary>
    public static RouteView Empty { get; } = new([], 0, 1);
}
