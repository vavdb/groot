namespace Groot.UI.Theme;

/// <summary>
/// The Groot colour palette: every design token (name, light value, dark value). A token's
/// CSS variable is --g-&lt;name&gt;. Consumers read from this: GrootTheme maps semantic roles
/// to tokens, tokens.css is generated from these tokens at build time, and the gallery
/// renders swatches from the same list.
/// </summary>
public static class GrootPalette
{
    /// <summary>One design token: name, light value, dark value. CSS var is --g-{Name}.</summary>
    public sealed record Token(string Name, string Light, string Dark);

    /// <summary>Every token, in the order tokens.css emits them. The canonical palette.</summary>
    public static IReadOnlyList<Token> All { get; } =
    [
        // Core surfaces + text + lines
        new("bg",        "#f4f1e8", "#151a12"),
        new("card",      "#fdfcf7", "#1d2419"),
        new("line",      "#ddd6c3", "#33402c"),
        new("ink",       "#26301f", "#e8ead9"),
        new("dim",       "#5b6650", "#a3b197"),
        // Brand / semantic colours
        new("moss",      "#4a7048", "#7fb47a"),
        new("moss-deep", "#2f4d2e", "#a5cf9f"),
        new("bark",      "#6b4f35", "#c49a6c"),
        new("amber",     "#c98f2d", "#e0aa4a"),
        new("clay",      "#b45f3c", "#d97e56"),
        // Signature-visual accents (CSS-only; no MudBlazor role)
        new("ring",      "#8aa385", "#4a5f45"),
        new("wither",    "#c9c2ae", "#3a4234"),
        new("run-ink",   "#241c08", "#241c08"),
        new("walk-ink",  "#f2f5ec", "#151a12"),
        new("shadow",    "0 18px 60px rgba(38, 48, 31, .14)", "0 18px 60px rgba(0, 0, 0, .5)"),
    ];

    /// <summary>Light value for a token, or throw if the name is unknown.</summary>
    public static string Light(string name) => Find(name).Light;

    /// <summary>Dark value for a token, or throw if the name is unknown.</summary>
    public static string Dark(string name) => Find(name).Dark;

    private static Token Find(string name) =>
        All.FirstOrDefault(t => t.Name == name)
        ?? throw new KeyNotFoundException($"Unknown Groot palette token '{name}'.");
}
