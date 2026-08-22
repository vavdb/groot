namespace Groot.UI.Theme;

/// <summary>
/// The Groot design tokens, in one place. Three lists feed everything else:
/// <see cref="All"/> (colours per theme), <see cref="Scale"/> (fonts, sizes, tracking;
/// theme-independent) and <see cref="MudRoles"/> (which colour token backs which MudBlazor
/// palette variable). tokens.css is generated from these at build time, GrootTheme maps the
/// same roles for MudBlazor's C# side, and the gallery renders from the same lists.
/// </summary>
public static class GrootPalette
{
    /// <summary>One colour token: name, light value, dark value. CSS var is --g-{Name}.</summary>
    public sealed record Token(string Name, string Light, string Dark);

    /// <summary>One theme-independent token (font family, size, tracking). CSS var is --g-{Name}.</summary>
    public sealed record FixedToken(string Name, string Value);

    /// <summary>MudBlazor palette variable (--mud-palette-{Mud}) backed by a colour token.</summary>
    public sealed record MudRole(string Mud, string Token);

    /// <summary>Every colour token, in the order tokens.css emits them.</summary>
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
        // Amber and clay used as text or as a thin stroke on bg/card. Light darkens them to
        // clear 4.5:1 (amber itself reads 2.5:1 there); dark keeps the base colour.
        // PaletteContrastTests pins both. Fills stay amber/clay with run-ink/card on top.
        new("amber-text", "#8f6212", "#e0aa4a"),
        new("clay-text",  "#a04f2e", "#d97e56"),
        // Signature-visual accents (CSS-only; no MudBlazor role)
        new("ring",      "#8aa385", "#4a5f45"),
        new("wither",    "#c9c2ae", "#3a4234"),
        new("run-ink",   "#241c08", "#241c08"),
        new("walk-ink",  "#f2f5ec", "#151a12"),
        new("shadow",    "0 18px 60px rgba(38, 48, 31, .14)", "0 18px 60px rgba(0, 0, 0, .5)"),
    ];

    /// <summary>
    /// Fonts and the type scale. No component sets a font-size in px; it picks a step here.
    /// The smallest step is 10px, the accessibility floor for labels (CLAUDE.md).
    /// </summary>
    public static IReadOnlyList<FixedToken> Scale { get; } =
    [
        new FixedToken("font-ui",      "Public Sans, system-ui, sans-serif"),
        new FixedToken("font-display", "Fraunces, Georgia, serif"),
        new FixedToken("text-2xs",  "10px"),
        new FixedToken("text-xs",   "11px"),
        new FixedToken("text-sm",   "12px"),
        new FixedToken("text-md",   "14px"),
        new FixedToken("text-lg",   "16px"),
        new FixedToken("text-xl",   "19px"),
        new FixedToken("text-2xl",  "26px"),
        new FixedToken("text-3xl",  "36px"),
        new FixedToken("text-4xl",  "42px"),
        new FixedToken("text-hero", "72px"),
        new FixedToken("track-caps", ".2em"),
        new FixedToken("track-wide", ".08em"),
    ];

    /// <summary>
    /// MudBlazor palette variables backed by our tokens, emitted per theme scope so MudBlazor
    /// chrome follows the nearest .theme-light / .theme-dark ancestor (the gallery renders both
    /// side by side under one provider). Keep in step with GrootTheme.
    /// </summary>
    public static IReadOnlyList<MudRole> MudRoles { get; } =
    [
        new MudRole("primary",            "amber"),
        new MudRole("primary-text",       "run-ink"),
        new MudRole("secondary",          "moss"),
        new MudRole("secondary-text",     "card"),
        new MudRole("tertiary",           "bark"),
        new MudRole("tertiary-text",      "card"),
        new MudRole("error",              "clay-text"),   // card on clay is 4.4:1 in light; clay-text clears 4.5
        new MudRole("error-text",         "card"),
        new MudRole("background",         "bg"),
        new MudRole("background-gray",    "bg"),
        new MudRole("surface",            "card"),
        new MudRole("appbar-background",  "card"),
        new MudRole("appbar-text",        "ink"),
        new MudRole("drawer-background",  "card"),
        new MudRole("drawer-text",        "ink"),
        new MudRole("drawer-icon",        "dim"),
        new MudRole("text-primary",       "ink"),
        new MudRole("text-secondary",     "dim"),
        new MudRole("action-default",     "dim"),
        new MudRole("lines-default",      "line"),
        new MudRole("lines-inputs",       "line"),
        new MudRole("divider",            "line"),
        new MudRole("table-lines",        "line"),
    ];

    /// <summary>Light value for a colour token, or throw if the name is unknown.</summary>
    public static string Light(string name) => Find(name).Light;

    /// <summary>Dark value for a colour token, or throw if the name is unknown.</summary>
    public static string Dark(string name) => Find(name).Dark;

    /// <summary>Value of a fixed token (font, size, tracking), or throw if unknown.</summary>
    public static string Fixed(string name) =>
        Scale.FirstOrDefault(t => t.Name == name)?.Value
        ?? throw new KeyNotFoundException($"Unknown Groot scale token '{name}'.");

    private static Token Find(string name) =>
        All.FirstOrDefault(t => t.Name == name)
        ?? throw new KeyNotFoundException($"Unknown Groot palette token '{name}'.");
}