using System.Reflection;
using System.Text.Json;
using Groot.Core.Intervals;

namespace Groot.Core.Programs;

/// <summary>
/// The built-in program definitions, read from the JSON embedded at build time
/// (<c>data/programs/*.json</c>). A new program is a new data file, never new code.
/// Parsing is hand-written against <see cref="JsonDocument"/> so it survives trimming
/// in the WASM head and fails loudly on a malformed definition.
/// </summary>
public sealed class ProgramCatalog
{
    private readonly IReadOnlyDictionary<string, IntervalProgram> _intervalPrograms;

    private ProgramCatalog(IReadOnlyList<ProgramSummary> programs, IReadOnlyList<IntervalProgram> intervalPrograms)
    {
        Programs = programs;
        IntervalPrograms = intervalPrograms;
        _intervalPrograms = intervalPrograms.ToDictionary(p => p.Id);
    }

    /// <summary>The catalog shipped inside Groot.Core. Parsed once.</summary>
    public static ProgramCatalog Embedded { get; } = LoadEmbedded();

    public IReadOnlyList<ProgramSummary> Programs { get; }

    public IReadOnlyList<IntervalProgram> IntervalPrograms { get; }

    public IntervalProgram IntervalProgram(string id) =>
        _intervalPrograms.TryGetValue(id, out var program)
            ? program
            : throw new KeyNotFoundException($"No interval program with id '{id}'.");

    public static ProgramCatalog Parse(IEnumerable<string> jsonDocuments)
    {
        var summaries = new List<ProgramSummary>();
        var intervals = new List<IntervalProgram>();

        foreach (var json in jsonDocuments)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var id = RequiredString(root, "id");
            var name = RequiredString(root, "name");
            var version = root.TryGetProperty("version", out var v) ? v.GetInt32() : 1;
            var type = ParseType(RequiredString(root, "type"), id);

            summaries.Add(new ProgramSummary(id, name, version, type));

            if (type == ProgramType.Intervals)
                intervals.Add(ParseIntervalProgram(root, id, name, version));
        }

        return new ProgramCatalog(
            summaries.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray(),
            intervals.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray());
    }

    private static ProgramCatalog LoadEmbedded()
    {
        var assembly = typeof(ProgramCatalog).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal);

        return Parse(names.Select(name => ReadResource(assembly, name)));
    }

    private static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded program '{name}' could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static ProgramType ParseType(string type, string id) => type switch
    {
        "intervals" => ProgramType.Intervals,
        "sets_reps" => ProgramType.SetsReps,
        _ => throw new InvalidOperationException($"Program '{id}' has unknown type '{type}'."),
    };

    private static IntervalProgram ParseIntervalProgram(JsonElement root, string id, string name, int version)
    {
        var defaults = ParseCueDefaults(root);

        if (!root.TryGetProperty("weeks", out var weeksElement) || weeksElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Interval program '{id}' has no weeks array.");

        var weeks = weeksElement.EnumerateArray().Select(w => ParseWeek(w, id)).ToArray();
        if (weeks.Length == 0)
            throw new InvalidOperationException($"Interval program '{id}' has no weeks.");

        return new IntervalProgram(id, name, version, defaults, weeks);
    }

    private static CueDefaults ParseCueDefaults(JsonElement root)
    {
        if (!root.TryGetProperty("cueDefaults", out var element))
            return new CueDefaults();

        var segmentStart = !element.TryGetProperty("segmentStartCue", out var s) || s.GetBoolean();
        var endingSoon = element.TryGetProperty("endingSoonCueAtSeconds", out var e) ? e.GetInt32() : -10;
        return new CueDefaults(segmentStart, endingSoon);
    }

    private static IntervalWeek ParseWeek(JsonElement element, string id)
    {
        var week = element.GetProperty("week").GetInt32();
        var sessions = element.TryGetProperty("sessionsPerWeek", out var s) ? s.GetInt32() : 3;

        var hasPlan = element.TryGetProperty("plan", out var planElement);
        var hasDays = element.TryGetProperty("days", out var daysElement);

        if (hasPlan == hasDays)
            throw new InvalidOperationException(
                $"Program '{id}' week {week} must declare exactly one of 'plan' or 'days'.");

        if (hasPlan)
            return new IntervalWeek(week, sessions, ParseSegments(planElement, id, week), Days: null);

        var days = daysElement.EnumerateArray()
            .Select(d => new IntervalDay(d.GetProperty("day").GetInt32(), ParseSegments(d.GetProperty("plan"), id, week)))
            .ToArray();

        if (days.Length == 0)
            throw new InvalidOperationException($"Program '{id}' week {week} has an empty days array.");

        return new IntervalWeek(week, sessions, Plan: null, days);
    }

    private static IReadOnlyList<Segment> ParseSegments(JsonElement element, string id, int week)
    {
        if (element.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Program '{id}' week {week} has a plan that is not an array.");

        var segments = element.EnumerateArray().Select(s => ParseSegment(s, id, week)).ToArray();
        if (segments.Length == 0)
            throw new InvalidOperationException($"Program '{id}' week {week} has an empty plan.");

        return segments;
    }

    private static Segment ParseSegment(JsonElement element, string id, int week)
    {
        var kind = RequiredString(element, "kind") switch
        {
            "walk" => SegmentKind.Walk,
            "run" => SegmentKind.Run,
            var other => throw new InvalidOperationException(
                $"Program '{id}' week {week} has unknown segment kind '{other}'."),
        };

        var seconds = element.GetProperty("seconds").GetInt32();
        if (seconds <= 0)
            throw new InvalidOperationException($"Program '{id}' week {week} has a segment of {seconds}s.");

        var label = element.TryGetProperty("label", out var l) ? l.GetString() : null;

        var cues = element.TryGetProperty("cues", out var cuesElement)
            ? cuesElement.EnumerateArray().Select(c => ParseCue(c, seconds, id, week)).ToArray()
            : null;

        return new Segment(kind, seconds, label, cues);
    }

    private static CuePoint ParseCue(JsonElement element, int segmentSeconds, string id, int week)
    {
        var at = element.GetProperty("at").GetInt32();
        if (at < -segmentSeconds || at > segmentSeconds)
            throw new InvalidOperationException(
                $"Program '{id}' week {week} has a cue at {at}s outside a {segmentSeconds}s segment.");
        return new CuePoint(at, RequiredString(element, "key"));
    }

    private static string RequiredString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.GetString() is { Length: > 0 } text
            ? text
            : throw new InvalidOperationException($"Program definition is missing '{property}'.");
}
