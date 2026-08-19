// Reads the token list out of src/Groot.UI/Theme/GrootPalette.cs and writes the
// matching .theme-light/.theme-dark CSS custom properties to tokens.css.
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

var tokens = tokenPattern.Matches(source)
    .Select(m => new Token(m.Groups["name"].Value, m.Groups["light"].Value, m.Groups["dark"].Value))
    .ToList();

if (tokens.Count == 0)
{
    Console.Error.WriteLine("no tokens parsed from palette source - check the new(\"name\", \"light\", \"dark\") format.");
    return 1;
}

var light = new StringBuilder();
var dark = new StringBuilder();
foreach (var t in tokens)
{
    light.Append("  --g-").Append(t.Name).Append(": ").Append(t.Light).AppendLine(";");
    dark.Append("  --g-").Append(t.Name).Append(": ").Append(t.Dark).AppendLine(";");
}

var sb = new StringBuilder();
sb.AppendLine("/* Groot design tokens - GENERATED from GrootPalette.cs by tools/Groot.Palette.Generator.");
sb.AppendLine("   Do not edit by hand; edit GrootPalette.All instead and rebuild.");
sb.AppendLine("   Scope a subtree with .theme-light / .theme-dark; components only use var(). */");
sb.AppendLine();
sb.AppendLine(".theme-light {");
sb.Append(light);
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine(".theme-dark {");
sb.Append(dark);
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine(".theme-light, .theme-dark {");
sb.AppendLine("  background: var(--g-bg);");
sb.AppendLine("  color: var(--g-ink);");
sb.AppendLine("  /* Fraunces/Public Sans arrive via the host page (self-hosted in production); these are fallbacks */");
sb.AppendLine("  font-family: \"Public Sans\", system-ui, sans-serif;");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine(".g-serif {");
sb.AppendLine("  font-family: \"Fraunces\", Georgia, serif;");
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
Console.WriteLine($"wrote {tokens.Count} tokens to {outputPath}");
return 0;

internal sealed record Token(string Name, string Light, string Dark);
