namespace Groot.UI.Theme;

/// <summary>
/// How numbers reach the screen. One place, because the same weight was being written eleven
/// different ways: <c>0.##</c> in the lifting screen, <c>N0</c> in the gallery, and " kg" hand
/// appended at every one of them. A format copied per call site is a format that drifts.
/// </summary>
public static class GrootFormat
{
    /// <summary>
    /// A weight in kilograms. <c>#,##0.##</c> covers everything the app shows: 1.25 kg for the
    /// smallest plate, 62.5 kg on the bar, and 118,000 kg for a year, without trailing zeros and
    /// without a second format for the large end.
    /// </summary>
    public static string Kilograms(decimal kilograms) => $"{kilograms:#,##0.##} kg";

    /// <summary>The number alone, same rounding, for places that supply their own unit.</summary>
    public static string Number(decimal value) => $"{value:#,##0.##}";

    /// <summary>Minutes and seconds, as a clock reads them: 5:00, 0:07, 12:30.</summary>
    public static string Clock(int totalSeconds) => $"{totalSeconds / 60}:{totalSeconds % 60:00}";
}
