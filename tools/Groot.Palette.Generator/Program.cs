// Reads the token lists out of src/Groot.UI/Theme/GrootPalette.cs and writes tokens.css:
// fixed tokens (fonts, type scale) on :root, colour tokens per .theme-light/.theme-dark scope,
// and the MudBlazor palette/typography variables mapped onto those tokens per scope.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: Groot.Palette.Generator <GrootPalette.cs> <tokens.css>");
    return 1;
}

var sourcePath = args[0];
var outputPath = args[1];

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"palette source not found: {sourcePath}");
    return 1;
}

var source = File.ReadAllText(sourcePath);

var tokenPattern = new Regex(
    "new\\(\"(?<name>[A-Za-z0-9_-]+)\"\\s*,\\s*\"(?<light>[^\"]+)\"\\s*,\\s*\"(?<dark>[^\"]+)\"\\)",
    RegexOptions.Compiled);
var fixedPattern = new Regex(
    "new FixedToken\\(\"(?<name>[A-Za-z0-9_-]+)\"\\s*,\\s*\"(?<value>[^\"]+)\"\\)",
    RegexOptions.Compiled);
var rolePattern = new Regex(
    "new MudRole\\(\"(?<mud>[A-Za-z0-9_-]+)\"\\s*,\\s*\"(?<token>[A-Za-z0-9_-]+)\"\\)",
    RegexOptions.Compiled);

var tokens = tokenPattern.Matches(source)
    .Select(m => new Token(m.Groups["name"].Value, m.Groups["light"].Value, m.Groups["dark"].Value))
    .ToList();
var fixedTokens = fixedPattern.Matches(source)
    .Select(m => new FixedToken(m.Groups["name"].Value, m.Groups["value"].Value))
    .ToList();
var roles = rolePattern.Matches(source)
    .Select(m => new MudRole(m.Groups["mud"].Value, m.Groups["token"].Value))
    .ToList();

if (tokens.Count == 0)
{
    Console.Error.WriteLine("no colour tokens parsed - check the new(\"name\", \"light\", \"dark\") format.");
    return 1;
}

var byName = tokens.ToDictionary(t => t.Name);
foreach (var role in roles)
{
    if (!byName.ContainsKey(role.Token))
    {
        Console.Error.WriteLine($"MudRole '{role.Mud}' points at unknown token '{role.Token}'.");
        return 1;
    }
}

// MudBlazor uses these -rgb variants for rgba() hover/ripple tints.
string[] rgbRoles = ["primary", "secondary", "tertiary", "error", "text-primary"];
string[] uiTypo = ["default", "body1", "body2", "button", "caption", "subtitle1", "subtitle2", "overline"];
string[] displayTypo = ["h1", "h2", "h3", "h4", "h5", "h6"];

var sb = new StringBuilder();
sb.AppendLine("/* Groot design tokens - GENERATED from GrootPalette.cs by tools/Groot.Palette.Generator.");
sb.AppendLine("   Do not edit by hand; edit GrootPalette instead and rebuild.");
sb.AppendLine("   Scope a subtree with .theme-light / .theme-dark; components only use var(). */");
sb.AppendLine();

sb.AppendLine("/* Fonts, type scale, tracking: theme-independent. */");
sb.AppendLine(":root {");
foreach (var f in fixedTokens)
    sb.Append("  --g-").Append(f.Name).Append(": ").Append(f.Value).AppendLine(";");
sb.AppendLine("}");
sb.AppendLine();

EmitScope("theme-light", t => t.Light);
EmitScope("theme-dark", t => t.Dark);

sb.AppendLine(".theme-light, .theme-dark {");
sb.AppendLine("  background: var(--g-bg);");
sb.AppendLine("  color: var(--g-ink);");
sb.AppendLine("  font-family: var(--g-font-ui);");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine(".g-serif {");
sb.AppendLine("  font-family: var(--g-font-display);");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine("/* Shared design primitives - used across heads and pages. */");
sb.AppendLine(".accent {");
sb.AppendLine("  color: var(--g-amber);");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine(".sub {");
sb.AppendLine("  color: var(--g-dim);");
sb.AppendLine("}");
sb.AppendLine();

File.WriteAllText(outputPath, sb.ToString());
Console.WriteLine($"wrote {tokens.Count} colour tokens, {fixedTokens.Count} fixed tokens, {roles.Count} Mud roles to {outputPath}");
return 0;

void EmitScope(string scope, Func<Token, string> pick)
{
    sb.Append('.').Append(scope).AppendLine(" {");
    foreach (var t in tokens)
        sb.Append("  --g-").Append(t.Name).Append(": ").Append(pick(t)).AppendLine(";");
    sb.AppendLine();
    sb.AppendLine("  /* MudBlazor palette, backed by the tokens above */");
    foreach (var role in roles)
    {
        sb.Append("  --mud-palette-").Append(role.Mud).Append(": var(--g-").Append(role.Token).AppendLine(");");
        if (rgbRoles.Contains(role.Mud) && TryRgb(pick(byName[role.Token]), out var rgb))
            sb.Append("  --mud-palette-").Append(role.Mud).Append("-rgb: ").Append(rgb).AppendLine(";");
    }
    sb.AppendLine();
    sb.AppendLine("  /* MudBlazor typography families follow our fonts */");
    foreach (var name in uiTypo)
        sb.Append("  --mud-typography-").Append(name).AppendLine("-family: var(--g-font-ui);");
    foreach (var name in displayTypo)
        sb.Append("  --mud-typography-").Append(name).AppendLine("-family: var(--g-font-display);");
    sb.AppendLine("}");
    sb.AppendLine();
}

static bool TryRgb(string hex, out string rgb)
{
    rgb = "";
    var h = hex.Trim();
    if (!h.StartsWith('#')) return false;
    h = h[1..];
    if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}"));
    if (h.Length != 6) return false;
    var r = int.Parse(h[..2], NumberStyles.HexNumber);
    var g = int.Parse(h[2..4], NumberStyles.HexNumber);
    var b = int.Parse(h[4..6], NumberStyles.HexNumber);
    rgb = $"{r}, {g}, {b}";
    return true;
}

internal sealed record Token(string Name, string Light, string Dark);
internal sealed record FixedToken(string Name, string Value);
internal sealed record MudRole(string Mud, string Token);