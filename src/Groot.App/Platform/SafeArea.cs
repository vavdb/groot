namespace Groot.App.Platform;

/// <summary>
/// How much of the window the system bars cover, in CSS pixels. Android draws the app from edge
/// to edge and its WebView reports env(safe-area-inset-*) as zero, so the shell is told the
/// measurements instead. Every other platform reports them in CSS and gets zero here.
/// </summary>
public static class SafeArea
{
    /// <summary>The status bar band along the top, and the display cutout where there is one.</summary>
    public static double Top => Measure().Top;

    /// <summary>The gesture bar or the button bar along the bottom.</summary>
    public static double Bottom => Measure().Bottom;

#if ANDROID
    private static (double Top, double Bottom) Measure()
    {
        var decor = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView;
        if (decor is null) return (0, 0);

        var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor);
        var bars = insets?.GetInsets(
            AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars()
            | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout());
        if (bars is null) return (0, 0);

        var density = decor.Resources?.DisplayMetrics?.Density ?? 1f;
        if (density <= 0) density = 1f;

        return (Math.Round(bars.Top / density, 1), Math.Round(bars.Bottom / density, 1));
    }
#else
    private static (double Top, double Bottom) Measure() => (0, 0);
#endif
}
