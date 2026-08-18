using MudBlazor;

namespace Groot.UI.Theme;

/// <summary>
/// MudBlazor theme for the chrome layer. Signature components ignore this and use
/// the CSS tokens in wwwroot/tokens.css; both are generated from the same palette.
/// </summary>
public static class GrootTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#4a7048",
            Secondary = "#c98f2d",
            Tertiary = "#6b4f35",
            Error = "#b45f3c",
            Background = "#f4f1e8",
            Surface = "#fdfcf7",
            AppbarBackground = "#fdfcf7",
            AppbarText = "#26301f",
            TextPrimary = "#26301f",
            TextSecondary = "#6f7a63",
            LinesDefault = "#ddd6c3",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7fb47a",
            Secondary = "#e0aa4a",
            Tertiary = "#c49a6c",
            Error = "#d97e56",
            Background = "#151a12",
            Surface = "#1d2419",
            AppbarBackground = "#1d2419",
            AppbarText = "#e8ead9",
            TextPrimary = "#e8ead9",
            TextSecondary = "#93a087",
            LinesDefault = "#33402c",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px",
        },
    };
}
