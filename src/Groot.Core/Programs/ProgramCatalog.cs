using System.Reflection;
using System.Text.Json;
using Groot.Core.Intervals;
using Groot.Core.Sessions;

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
    private readonly IReadOnlyDictionary<string, LiftProgram> _liftPrograms;

    private ProgramCatalog(
        IReadOnlyList<ProgramSummary> programs,
        IReadOnlyList<IntervalProgram> intervalPrograms,
        IReadOnlyList<LiftProgram> liftPrograms)
    {
        Programs = programs;
        IntervalPrograms = intervalPrograms;
        LiftPrograms = liftPrograms;
        _intervalPrograms = intervalPrograms.ToDictionary(p => p.Id);
        _liftPrograms = liftPrograms.ToDictionary(p => p.Id);
    }

    /// <summary>The catalog shipped inside Groot.Core. Parsed once.</summary>
    public static ProgramCatalog Embedded { get; } = LoadEmbedded();

    public IReadOnlyList<ProgramSummary> Programs { get; }

    public IReadOnlyList<IntervalProgram> IntervalPrograms { get; }

    public IReadOnlyList<LiftProgram> LiftPrograms { get; }

    public IntervalProgram IntervalProgram(string id) =>
        _intervalPrograms.TryGetValue(id, out var program)
            ? program
            : throw new KeyNotFoundException($"No interval program with id '{id}'.");

    public LiftProgram LiftProgram(string id) =>
        _liftPrograms.TryGetValue(id, out var program)
            ? program
            : throw new KeyNotFoundException($"No lift program with id '{id}'.");

    public static ProgramCatalog Parse(IEnumerable<string> jsonDocuments)
    {
        var summaries = new List<ProgramSummary>();
        var intervals = new List<IntervalProgram>();
        var lifts = new List<LiftProgram>();

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
            else
                lifts.Add(ParseLiftProgram(root, id, name, version));
        }

        return new ProgramCatalog(
            summaries.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray(),
            intervals.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray(),
            lifts.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray());
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

        var duplicateWeek = weeks.GroupBy(w => w.Week).FirstOrDefault(g => g.Count() > 1);
        if (duplicateWeek is not null)
            throw new InvalidOperationException($"Interval program '{id}' declares week {duplicateWeek.Key} more than once.");

        return new IntervalProgram(id, name, version, defaults, weeks);
    }


    private static LiftProgram ParseLiftProgram(JsonElement root, string id, string name, int version)
    {
        if (!root.TryGetProperty("days", out var daysElement) || daysElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Lift program '{id}' has no days object.");

        var days = daysElement.EnumerateObject()
            .Select(day => new LiftDay(day.Name, ParseLiftExercises(day.Value, id, day.Name)))
            .ToArray();

        if (days.Length == 0)
            throw new InvalidOperationException($"Lift program '{id}' has no days.");

        var rotation = root.TryGetProperty("rotation", out var rotationElement)
            ? rotationElement.EnumerateArray().Select(r => r.GetString() ?? "").ToArray()
            : days.Select(d => d.Key).ToArray();

        var unknownDay = rotation.FirstOrDefault(key => days.All(d => d.Key != key));
        if (unknownDay is not null)
            throw new InvalidOperationException($"Lift program '{id}' rotates through '{unknownDay}', which is not a day.");

        var sessions = root.TryGetProperty("sessionsPerWeek", out var s) ? s.GetInt32() : 3;
        if (sessions <= 0)
            throw new InvalidOperationException($"Lift program '{id}' has {sessions} sessionsPerWeek; must be positive.");

        return new LiftProgram(
            id, name, version, sessions, rotation,
            ParseTiers(root, id),
            ParseRestSeconds(root, id),
            days);
    }

    private static IReadOnlyList<LiftExercise> ParseLiftExercises(JsonElement element, string id, string dayKey)
    {
        if (element.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Lift program '{id}' day '{dayKey}' is not an array.");

        var exercises = element.EnumerateArray().Select(e =>
        {
            var tier = e.GetProperty("tier").GetInt32();
            if (tier is < 1 or > 3)
                throw new InvalidOperationException($"Lift program '{id}' day '{dayKey}' has tier {tier}; tiers are 1 to 3.");

            var exerciseId = RequiredString(e, "exercise");
            var loading = e.TryGetProperty("loading", out var l)
                ? ParseLoading(l.GetString(), id, exerciseId)
                : LoadingKind.Barbell;

            return new LiftExercise(exerciseId, tier, loading);
        }).ToArray();

        if (exercises.Length == 0)
            throw new InvalidOperationException($"Lift program '{id}' day '{dayKey}' has no exercises.");

        // A day that names the same exercise twice would share one set of logged sets and one
        // progression outcome on the screen. Refuse it here, the way duplicate weeks are refused.
        var duplicate = exercises.GroupBy(e => e.ExerciseId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException(
                $"Lift program '{id}' day '{dayKey}' lists '{duplicate.Key}' more than once.");

        return exercises;
    }

    private static LoadingKind ParseLoading(string? loading, string id, string exerciseId) => loading switch
    {
        "barbell" => LoadingKind.Barbell,
        "dumbbell" => LoadingKind.Dumbbell,
        "bodyweight" => LoadingKind.Bodyweight,
        _ => throw new InvalidOperationException(
            $"Lift program '{id}' loads '{exerciseId}' as '{loading}'; expected barbell, dumbbell or bodyweight."),
    };

    /// <summary>Reads progression.T1 and its siblings: scheme, increments, fail ladder, reset.</summary>
    private static IReadOnlyDictionary<int, TierProgression> ParseTiers(JsonElement root, string id)
    {
        if (!root.TryGetProperty("progression", out var progression) || progression.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Lift program '{id}' has no progression object.");

        var tiers = new Dictionary<int, TierProgression>();
        foreach (var tier in progression.EnumerateObject())
        {
            if (tier.Name.Length != 2 || tier.Name[0] != 'T' || !int.TryParse(tier.Name[1..], out var number))
                throw new InvalidOperationException($"Lift program '{id}' has a progression key '{tier.Name}'; expected T1, T2 or T3.");

            tiers[number] = ParseTier(tier.Value, id, tier.Name);
        }

        if (tiers.Count == 0)
            throw new InvalidOperationException($"Lift program '{id}' declares no tiers.");

        return tiers;
    }

    private static TierProgression ParseTier(JsonElement element, string id, string tierName)
    {
        var scheme = SetScheme.Parse(RequiredString(element, "scheme"));

        var increment = 0m;
        var overrides = new Dictionary<string, decimal>();
        if (element.TryGetProperty("incrementKg", out var incrementElement))
        {
            foreach (var entry in incrementElement.EnumerateObject())
            {
                var kg = entry.Value.GetDecimal();
                if (kg < 0m)
                    throw new InvalidOperationException($"Lift program '{id}' {tierName} increments by {kg} kg.");

                if (entry.Name == "default") increment = kg;
                else overrides[entry.Name] = kg;
            }
        }

        if (element.TryGetProperty("incrementKg", out _) && increment == 0m)
            throw new InvalidOperationException(
                $"Lift program '{id}' {tierName} declares incrementKg without a default; a clean session would add nothing.");

        var ladder = element.TryGetProperty("failLadder", out var ladderElement)
            ? ladderElement.EnumerateArray().Select(rung => SetScheme.Parse(rung.GetString() ?? "")).ToArray()
            : [];

        decimal? resetPct = null;
        decimal? resetBump = null;
        if (element.TryGetProperty("afterLadderReset", out var reset))
        {
            if (reset.TryGetProperty("toPctOfLast5x3", out var pct)) resetPct = pct.GetDecimal();
            if (reset.TryGetProperty("bumpKg", out var bump)) resetBump = bump.GetDecimal();
        }

        int? threshold = element.TryGetProperty("progressAtTotalReps", out var reps) ? reps.GetInt32() : null;

        return new TierProgression(scheme, increment, overrides, ladder, resetPct, resetBump, threshold);
    }

    private static IReadOnlyDictionary<int, int> ParseRestSeconds(JsonElement root, string id)
    {
        if (!root.TryGetProperty("restSeconds", out var element) || element.ValueKind != JsonValueKind.Object)
            return new Dictionary<int, int>();

        var rest = new Dictionary<int, int>();
        foreach (var tier in element.EnumerateObject())
        {
            if (!int.TryParse(tier.Name, out var number))
                throw new InvalidOperationException($"Lift program '{id}' has a restSeconds key '{tier.Name}'; expected a tier number.");

            var seconds = tier.Value.GetInt32();
            if (seconds < 0)
                throw new InvalidOperationException($"Lift program '{id}' rests {seconds}s at tier {number}.");

            rest[number] = seconds;
        }

        return rest;
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
        if (week <= 0)
            throw new InvalidOperationException($"Program '{id}' has a week number of {week}; weeks must be positive.");

        var sessions = element.TryGetProperty("sessionsPerWeek", out var s) ? s.GetInt32() : 3;
        if (sessions <= 0)
            throw new InvalidOperationException($"Program '{id}' week {week} has {sessions} sessionsPerWeek; must be positive.");

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

        var badDay = days.FirstOrDefault(d => d.Day <= 0 || d.Day > sessions);
        if (badDay is not null)
            throw new InvalidOperationException(
                $"Program '{id}' week {week} has a day number of {badDay.Day}; must be within 1..{sessions}.");

        var duplicateDay = days.GroupBy(d => d.Day).FirstOrDefault(g => g.Count() > 1);
        if (duplicateDay is not null)
            throw new InvalidOperationException($"Program '{id}' week {week} declares day {duplicateDay.Key} more than once.");

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
