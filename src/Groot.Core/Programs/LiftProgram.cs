namespace Groot.Core.Programs;

/// <summary>
/// A sets-and-reps scheme as programs write it: "5x3+" is five sets of three with the last set
/// taken as many reps as possible, "3x10" is three straight sets of ten.
/// </summary>
public sealed record SetScheme(int Sets, int Reps, bool AmrapLast)
{
    /// <summary>Parses "5x3+", "3x10", "10x1+". Throws on anything else, loudly and early.</summary>
    public static SetScheme Parse(string text)
    {
        var trimmed = text.Trim();
        var amrap = trimmed.EndsWith('+');
        var core = amrap ? trimmed[..^1] : trimmed;
        var parts = core.Split('x', StringSplitOptions.TrimEntries);

        if (parts.Length != 2
            || !int.TryParse(parts[0], out var sets) || sets <= 0
            || !int.TryParse(parts[1], out var reps) || reps <= 0)
        {
            throw new FormatException($"'{text}' is not a set scheme; expected forms are 5x3+, 3x10, 10x1+.");
        }

        return new SetScheme(sets, reps, amrap);
    }

    /// <summary>The scheme as a program writes it, so a screen can show the same string back.</summary>
    public string Text => $"{Sets}x{Reps}{(AmrapLast ? "+" : "")}";
}

/// <summary>
/// How a tier progresses: the scheme it starts on, what a good session adds, the schemes it drops
/// through after a failure, and what happens once the ladder runs out. GZCLP resets T1 to a
/// percentage of the last base weight and bumps T2 by a fixed amount; T3 climbs when its AMRAP
/// set reaches a rep threshold.
/// </summary>
public sealed record TierProgression(
    SetScheme Scheme,
    decimal IncrementKg,
    IReadOnlyDictionary<string, decimal> IncrementOverrides,
    IReadOnlyList<SetScheme> FailLadder,
    decimal? ResetPctOfLast = null,
    decimal? ResetBumpKg = null,
    int? ProgressAtAmrapReps = null)
{
    /// <summary>The increment for one exercise: its own if the program names it, the tier's otherwise.</summary>
    public decimal IncrementFor(string exerciseId) =>
        IncrementOverrides.TryGetValue(exerciseId, out var kg) ? kg : IncrementKg;

    /// <summary>The scheme at a rung of the ladder: stage 0 is the tier's own scheme.</summary>
    public SetScheme SchemeAt(int stage) =>
        stage <= 0 ? Scheme
        : stage <= FailLadder.Count ? FailLadder[stage - 1]
        : Scheme;
}

/// <summary>
/// How an exercise is loaded, which decides what its weight means: a total on the bar, the weight
/// in each hand, or an adjustment to bodyweight (added, or taken off by an assist).
/// </summary>
public enum LoadingKind { Barbell, Dumbbell, Bodyweight }

/// <summary>One exercise slot on a training day: which exercise, at which tier, loaded how.</summary>
public sealed record LiftExercise(string ExerciseId, int Tier, LoadingKind Loading = LoadingKind.Barbell);

/// <summary>One day of the rotation ("A1"), in the order the exercises are trained.</summary>
public sealed record LiftDay(string Key, IReadOnlyList<LiftExercise> Exercises);

/// <summary>
/// A sets-and-reps program: the rotation of training days, the scheme and rest each tier uses,
/// and what each day holds. Weights are not in here; they belong to the lifter, not the program.
/// </summary>
public sealed record LiftProgram(
    string Id,
    string Name,
    int Version,
    int SessionsPerWeek,
    IReadOnlyList<string> Rotation,
    IReadOnlyDictionary<int, TierProgression> Tiers,
    IReadOnlyDictionary<int, int> RestSeconds,
    IReadOnlyList<LiftDay> Days)
{
    public ProgramSummary Summary => new(Id, Name, Version, ProgramType.SetsReps);

    public LiftDay Day(string key) =>
        Days.FirstOrDefault(d => d.Key == key)
        ?? throw new ArgumentOutOfRangeException(nameof(key), key, $"Program {Id} has no day '{key}'.");

    /// <summary>The day at a position in the rotation, counting from zero and wrapping.</summary>
    public LiftDay DayAt(int rotationIndex) =>
        Day(Rotation[((rotationIndex % Rotation.Count) + Rotation.Count) % Rotation.Count]);

    /// <summary>
    /// The rotation day that follows <paramref name="dayKey"/>, wrapping at the end. GZCLP runs
    /// A1, B1, A2, B2 and then starts over, so what comes next is a property of the program, not
    /// of the calendar: three sessions a week means the rotation and the week drift apart on
    /// purpose.
    /// </summary>
    public string NextDayAfter(string dayKey)
    {
        var index = Rotation.ToList().IndexOf(dayKey);
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(dayKey), dayKey, $"Program {Id} has no day '{dayKey}' in its rotation.");

        return Rotation[(index + 1) % Rotation.Count];
    }

    /// <summary>
    /// Whether this day is still in the rotation. A program's rotation can change between
    /// versions, so a caller resuming from history has to ask before it follows what was logged.
    /// </summary>
    public bool Rotates(string dayKey) => Rotation.Contains(dayKey);

    /// <summary>Where a lifter starts a program that has no history yet.</summary>
    public string FirstDay => Rotation[0];

    public TierProgression TierFor(int tier) =>
        Tiers.TryGetValue(tier, out var progression)
            ? progression
            : throw new ArgumentOutOfRangeException(nameof(tier), tier, $"Program {Id} has no tier {tier}.");

    public SetScheme SchemeFor(int tier) => TierFor(tier).Scheme;

    /// <summary>Rest between sets at a tier. Tiers without a declared rest get 90 seconds.</summary>
    public int RestSecondsFor(int tier) => RestSeconds.TryGetValue(tier, out var seconds) ? seconds : 90;
}
