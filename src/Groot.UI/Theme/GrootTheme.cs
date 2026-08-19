using MudBlazor;

namespace Groot.UI.Theme;

/// <summary>
/// The MudBlazor theme that skins Groot's chrome (forms, buttons, cards, dialogs, tables).
/// Colours resolve from <see cref="GrootPalette"/>: amber = Primary, moss = Secondary,
/// bark = Tertiary, clay = Error, with surfaces/text/lines from the bg/card/ink/dim/line tokens.
/// Body text uses Public Sans; headings use Fraunces.
/// </summary>
public static class GrootTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = GrootPalette.Light("amber"),
            Secondary = GrootPalette.Light("moss"),
            Tertiary = GrootPalette.Light("bark"),
            Error = GrootPalette.Light("clay"),
            Background = GrootPalette.Light("bg"),
            Surface = GrootPalette.Light("card"),
            AppbarBackground = GrootPalette.Light("card"),
            AppbarText = GrootPalette.Light("ink"),
            DrawerBackground = GrootPalette.Light("card"),
            DrawerText = GrootPalette.Light("ink"),
            TextPrimary = GrootPalette.Light("ink"),
            TextSecondary = GrootPalette.Light("dim"),
            LinesDefault = GrootPalette.Light("line"),
            LinesInputs = GrootPalette.Light("line"),
        },
        PaletteDark = new PaletteDark
        {
            Primary = GrootPalette.Dark("amber"),
            Secondary = GrootPalette.Dark("moss"),
            Tertiary = GrootPalette.Dark("bark"),
            Error = GrootPalette.Dark("clay"),
            Background = GrootPalette.Dark("bg"),
            Surface = GrootPalette.Dark("card"),
            AppbarBackground = GrootPalette.Dark("card"),
            AppbarText = GrootPalette.Dark("ink"),
            DrawerBackground = GrootPalette.Dark("card"),
            DrawerText = GrootPalette.Dark("ink"),
            TextPrimary = GrootPalette.Dark("ink"),
            TextSecondary = GrootPalette.Dark("dim"),
            LinesDefault = GrootPalette.Dark("line"),
            LinesInputs = GrootPalette.Dark("line"),
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Public Sans", "system-ui", "sans-serif" },
            },
            H6 = new H6Typography
            {
                FontFamily = new[] { "Fraunces", "Georgia", "serif" },
                FontWeight = "900",
            },
        },
    };
}
