namespace Groot.Core.Health;

/// <summary>Distance between two positions on the ground.</summary>
public static class Geo
{
    /// <summary>Mean Earth radius, the one the haversine formula assumes.</summary>
    public const double EarthRadiusMetres = 6_371_008.8;

    /// <summary>
    /// Great-circle distance in metres. Haversine rather than a flat approximation: the flat one
    /// is fine over a kilometre and wrong near the poles, and this is not a hot path.
    /// </summary>
    public static double DistanceMetres(double latitudeA, double longitudeA, double latitudeB, double longitudeB)
    {
        var dLat = Radians(latitudeB - latitudeA);
        var dLon = Radians(longitudeB - longitudeA);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Radians(latitudeA)) * Math.Cos(Radians(latitudeB))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * EarthRadiusMetres * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
