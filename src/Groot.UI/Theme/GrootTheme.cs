using MudBlazor;

namespace Groot.UI.Theme;

/// <summary>
/// The MudBlazor theme that skins Groot's chrome (forms, buttons, cards, dialogs, tables).
/// Colours resolve from <see cref="GrootPalette"/>: amber = Primary, moss = Secondary,
/// bark = Tertiary, clay-text = Error, with surfaces/text/lines from the bg/card/ink/dim/line
/// tokens. Keep in step with <see cref="GrootPalette.MudRoles"/>, which emits the same
/// mapping as CSS variables per theme scope.
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
            Error = GrootPalette.Light("clay-text"),
            Background = GrootPalette.Light("bg"),
            Surface = GrootPalette.Light("card"),
            AppbarBackground = GrootPalette.Light("card"),
            AppbarText = GrootPalette.Light("ink"),
            DrawerBackground = GrootPalette.Light("card"),
            DrawerText = GrootPalette.Light("ink"),
            TextPrimary = GrootPalette.Light("ink"),
            TextSecondary = GrootPalette.Light("dim"),
            // What a button or icon with no colour of its own draws in. MudBlazor's own default
            // is a white with an alpha, which read as a different white beside the palette's ink.
            ActionDefault = GrootPalette.Light("ink"),
            LinesDefault = GrootPalette.Light("line"),
            LinesInputs = GrootPalette.Light("line"),
        },
        PaletteDark = new PaletteDark
        {
            Primary = GrootPalette.Dark("amber"),
            Secondary = GrootPalette.Dark("moss"),
            Tertiary = GrootPalette.Dark("bark"),
            Error = GrootPalette.Dark("clay-text"),
            Background = GrootPalette.Dark("bg"),
            Surface = GrootPalette.Dark("card"),
            AppbarBackground = GrootPalette.Dark("card"),
            AppbarText = GrootPalette.Dark("ink"),
            DrawerBackground = GrootPalette.Dark("card"),
            DrawerText = GrootPalette.Dark("ink"),
            TextPrimary = GrootPalette.Dark("ink"),
            TextSecondary = GrootPalette.Dark("dim"),
            // What a button or icon with no colour of its own draws in. MudBlazor's own default
            // is a white with an alpha, which read as a different white beside the palette's ink.
            ActionDefault = GrootPalette.Dark("ink"),
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
            // Fraunces ships at 500, 700 and 900. MudBlazor's own heading weights are lighter
            // than that (h1 is 300), so a heading asking for one gets a synthesised face or the
            // nearest cut. Every serif heading role is pinned to a weight the font actually has.
            H1 = new H1Typography
            {
                FontFamily = new[] { "Fraunces", "Georgia", "serif" },
                FontWeight = "900",
            },
            H4 = new H4Typography
            {
                FontFamily = new[] { "Fraunces", "Georgia", "serif" },
                FontWeight = "900",
            },
            H5 = new H5Typography
            {
                FontFamily = new[] { "Fraunces", "Georgia", "serif" },
                FontWeight = "900",
            },
            H6 = new H6Typography
            {
                FontFamily = new[] { "Fraunces", "Georgia", "serif" },
                FontWeight = "900",
            },
        },
    };
}
