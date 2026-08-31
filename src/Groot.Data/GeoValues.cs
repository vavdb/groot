namespace Groot.Data;

/// <summary>
/// How positions cross the storage boundary. Degrees go in and out as whole ten-millionths, the
/// form Android's location APIs already use: about a centimetre of resolution, exact in an
/// INTEGER column, and free of the drift a REAL column would add on every round trip.
/// </summary>
internal static class GeoValues
{
    private const double DegreeScale = 10_000_000;

    /// <summary>Degrees to whole ten-millionths of a degree.</summary>
    public static long ToE7(double degrees) => (long)Math.Round(degrees * DegreeScale, MidpointRounding.AwayFromZero);

    /// <summary>Whole ten-millionths of a degree back to degrees.</summary>
    public static double FromE7(long e7) => e7 / DegreeScale;

    /// <summary>
    /// Metres to whole centimetres, never below one: the column requires a positive accuracy,
    /// and a device claiming better than a centimetre is claiming more than GPS can deliver.
    /// </summary>
    public static long ToCentimetres(double metres) =>
        Math.Max(1, (long)Math.Round(metres * 100, MidpointRounding.AwayFromZero));

    /// <summary>Whole centimetres back to metres.</summary>
    public static double FromCentimetres(long centimetres) => centimetres / 100.0;
}
