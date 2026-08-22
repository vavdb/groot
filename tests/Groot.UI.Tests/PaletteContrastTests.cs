using Groot.UI.Theme;

namespace Groot.UI.Tests;

/// <summary>
/// WCAG AA is the floor (AGENTS.md): text 4.5:1, large text and UI graphics 3:1, measured
/// against the real background token. Every pair here is a combination a component renders.
/// A new text colour, a new surface, or a new place an accent is used as text is a new row.
/// </summary>
public class PaletteContrastTests
{
    private const double Text = 4.5;
    private const double Graphic = 3.0;

    /// <summary>foreground token, background token, minimum ratio, where the pair is rendered.</summary>
    public static TheoryData<string, string, double, string> Pairs => new()
    {
        { "ink", "bg", Text, "body text on the page" },
        { "ink", "card", Text, "body text on cards" },
        { "dim", "bg", Text, "captions and meters on the page" },
        { "dim", "card", Text, "day names, pending slots and set circles on cards" },
        { "moss", "card", Text, "meter ok, run summary" },
        { "moss", "bg", Text, "walk accents on the page" },
        { "bark", "card", Text, "tertiary text" },
        { "amber-text", "bg", Text, "amber as text on the page" },
        { "amber-text", "card", Text, "today slot caption, active set number" },
        { "clay-text", "bg", Text, "warning text on the page" },
        { "clay-text", "card", Text, "joker slot caption, jokers left" },
        { "card", "moss", Text, "lift slot and done set circle" },
        { "run-ink", "amber", Text, "run slot and run flood" },
        { "walk-ink", "moss", Text, "walk flood" },
        { "amber-text", "card", Graphic, "active set ring, today slot border, session dots" },
        { "clay-text", "card", Graphic, "joker slot border" },
        { "moss", "card", Graphic, "summary border" },
    };

    /// <summary>MudBlazor text roles and the fill role they sit on.</summary>
    public static TheoryData<string, string> MudTextOnFill => new()
    {
        { "primary-text", "primary" },
        { "secondary-text", "secondary" },
        { "tertiary-text", "tertiary" },
        { "error-text", "error" },
        { "text-primary", "background" },
        { "text-primary", "surface" },
        { "text-secondary", "background" },
        { "text-secondary", "surface" },
        { "appbar-text", "appbar-background" },
        { "drawer-text", "drawer-background" },
        { "drawer-icon", "drawer-background" },
    };

    [Theory]
    [MemberData(nameof(Pairs))]
    public void Light_pair_meets_its_minimum(string fg, string bg, double minimum, string use) =>
        AssertRatio(GrootPalette.Light(fg), GrootPalette.Light(bg), minimum, $"{fg} on {bg}, light: {use}");

    [Theory]
    [MemberData(nameof(Pairs))]
    public void Dark_pair_meets_its_minimum(string fg, string bg, double minimum, string use) =>
        AssertRatio(GrootPalette.Dark(fg), GrootPalette.Dark(bg), minimum, $"{fg} on {bg}, dark: {use}");

    [Theory]
    [MemberData(nameof(MudTextOnFill))]
    public void Mud_text_role_reads_on_its_fill(string textRole, string fillRole)
    {
        var text = TokenFor(textRole);
        var fill = TokenFor(fillRole);

        AssertRatio(GrootPalette.Light(text), GrootPalette.Light(fill), Text, $"--mud-palette-{textRole} on --mud-palette-{fillRole}, light");
        AssertRatio(GrootPalette.Dark(text), GrootPalette.Dark(fill), Text, $"--mud-palette-{textRole} on --mud-palette-{fillRole}, dark");
    }

    [Fact]
    public void Every_mud_role_points_at_a_colour_token()
    {
        var names = GrootPalette.All.Select(t => t.Name).ToHashSet();
        var dangling = GrootPalette.MudRoles.Where(r => !names.Contains(r.Token)).Select(r => $"{r.Mud} -> {r.Token}").ToArray();

        Assert.Empty(dangling);
    }

    [Fact]
    public void Type_scale_never_goes_below_ten_pixels()
    {
        var steps = GrootPalette.Scale.Where(t => t.Name.StartsWith("text-", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(steps);
        Assert.All(steps, step =>
        {
            Assert.EndsWith("px", step.Value);
            var px = int.Parse(step.Value[..^2], System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(px >= 10, $"--g-{step.Name} is {px}px; labels and captions stay at 10px or more");
        });
    }

    [Fact]
    public void Text_safe_accents_match_their_base_in_dark_theme()
    {
        // Dark theme accents already clear 4.5:1, so the text variants are the same colour
        // there; only the light theme darkens them. Keeps the two from drifting apart.
        Assert.Equal(GrootPalette.Dark("amber"), GrootPalette.Dark("amber-text"));
        Assert.Equal(GrootPalette.Dark("clay"), GrootPalette.Dark("clay-text"));
    }

    private static string TokenFor(string mudRole) =>
        GrootPalette.MudRoles.FirstOrDefault(r => r.Mud == mudRole)?.Token
        ?? throw new KeyNotFoundException($"No MudRole '{mudRole}' in GrootPalette.MudRoles.");

    private static void AssertRatio(string fg, string bg, double minimum, string what)
    {
        var ratio = Contrast(fg, bg);
        Assert.True(ratio >= minimum, $"{what}: {fg} on {bg} is {ratio:0.00}:1, minimum {minimum}:1");
    }

    /// <summary>WCAG 2.x contrast ratio between two 6-digit hex colours.</summary>
    internal static double Contrast(string hexA, string hexB)
    {
        var a = Luminance(hexA);
        var b = Luminance(hexB);
        var (hi, lo) = a >= b ? (a, b) : (b, a);
        return (hi + 0.05) / (lo + 0.05);
    }

    private static double Luminance(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length != 6)
            throw new ArgumentException($"'{hex}' is not a 6-digit hex colour; only solid colours can be measured.", nameof(hex));

        return 0.2126 * Channel(h, 0) + 0.7152 * Channel(h, 2) + 0.0722 * Channel(h, 4);
    }

    private static double Channel(string hex, int offset)
    {
        var c = Convert.ToInt32(hex.Substring(offset, 2), 16) / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
