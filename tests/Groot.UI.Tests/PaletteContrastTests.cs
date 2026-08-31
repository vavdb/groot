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
        // The five heart rate zones, drawn as a 3px stroke over the run/walk bands. The bands
        // are tints of amber and ring at 18% or less, so the page background is what the stroke
        // has to read against.
        { "dim", "bg", Graphic, "heart rate trace, easy zone" },
        { "moss", "bg", Graphic, "heart rate trace, steady zone" },
        { "amber-text", "bg", Graphic, "heart rate trace, moderate zone" },
        { "clay-text", "bg", Graphic, "heart rate trace, hard zone" },
        { "pulse-peak", "bg", Graphic, "heart rate trace, maximum zone" },
    };

    /// <summary>
    /// Zone colours that must stay apart from each other, not just from the background. clay-text
    /// collapses onto clay in the dark theme, which is why the top zone has a token of its own.
    /// </summary>
    public static TheoryData<string, string> DistinctZonePairs => new()
    {
        { "dim", "moss" },
        { "moss", "amber-text" },
        { "amber-text", "clay-text" },
        { "clay-text", "pulse-peak" },
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

    [Theory]
    [MemberData(nameof(DistinctZonePairs))]
    public void Neighbouring_zone_colours_stay_apart_in_both_themes(string lower, string higher)
    {
        // Perceptual distance, not contrast ratio: the WCAG ratio is a luminance comparison, and
        // moss against amber-text is 1.06:1 there while being obviously two different colours.
        // Telling one stroke from the next is a hue question, so it needs a hue-aware measure.
        AssertDistinct(GrootPalette.Light(lower), GrootPalette.Light(higher), $"{lower} beside {higher}, light");
        AssertDistinct(GrootPalette.Dark(lower), GrootPalette.Dark(higher), $"{lower} beside {higher}, dark");
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

    /// <summary>
    /// Two colours a reader has to tell apart when they sit next to each other. 15 is the usual
    /// "clearly a different colour" mark on this scale; a just-noticeable difference is about 2.
    /// </summary>
    private static void AssertDistinct(string a, string b, string what)
    {
        const double MinimumDifference = 15;

        var difference = Difference(a, b);
        Assert.True(difference >= MinimumDifference,
            $"{what}: {a} and {b} differ by {difference:0.0}, minimum {MinimumDifference}");
    }

    /// <summary>
    /// CIE76 colour difference between two 6-digit hex colours, by way of sRGB to XYZ to L*a*b*.
    /// Unlike the WCAG ratio this notices hue, which is what separates one zone stroke from the
    /// next: two colours can sit at the same lightness and still be plainly different colours.
    /// </summary>
    internal static double Difference(string hexA, string hexB)
    {
        var (l1, a1, b1) = Lab(hexA);
        var (l2, a2, b2) = Lab(hexB);

        return Math.Sqrt((l1 - l2) * (l1 - l2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
    }

    private static (double L, double A, double B) Lab(string hex)
    {
        var (r, g, b) = LinearRgb(hex);

        // sRGB to CIE XYZ, D65.
        var x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047;
        var y = 0.2126 * r + 0.7152 * g + 0.0722 * b;
        var z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883;

        static double Pivot(double t) => t > 0.008856 ? Math.Cbrt(t) : (7.787 * t) + (16.0 / 116);

        var fx = Pivot(x);
        var fy = Pivot(y);
        var fz = Pivot(z);

        return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));
    }

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
        var (r, g, b) = LinearRgb(hex);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>A hex colour with the sRGB transfer curve undone, which both measures start from.</summary>
    private static (double R, double G, double B) LinearRgb(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length != 6)
            throw new ArgumentException($"'{hex}' is not a 6-digit hex colour; only solid colours can be measured.", nameof(hex));

        return (Channel(h, 0), Channel(h, 2), Channel(h, 4));
    }

    private static double Channel(string hex, int offset)
    {
        var c = Convert.ToInt32(hex.Substring(offset, 2), 16) / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
