using Groot.Core.Contract;
using Groot.Core.Equipment;
using Groot.Core.Sessions;

namespace Groot.Data.Sessions;

/// <summary>The sessions table, one property per column. Storage types only, no conversions.</summary>
internal sealed record SessionRow(
    string Id,
    string UserId,
    string Date,
    string Kind,
    string? ProgramId,
    string? DayKey,
    long? IntervalWeek,
    long? IntervalDay,
    long? DurationS,
    string? Notes,
    long UpdatedAt,
    string DeviceId,
    long Deleted);

/// <summary>The sets table, one property per column.</summary>
internal sealed record SetRow(
    string Id,
    string SessionId,
    string ExerciseId,
    long SetOrder,
    string WeightKg,
    long? Reps,
    string EntryMode,
    string EntryWeight,
    string EntryUnit,
    string? EquipmentId,
    long IsWarmup,
    string? Notes);

/// <summary>
/// The two-way translation between stored rows and the domain. It exists because the shapes
/// genuinely differ: <see cref="SetEntry"/> has no notion of a tombstone, and the session's
/// program fields mean different things per kind.
/// </summary>
internal static class SessionMapping
{
    /// <summary>The kind as the CHECK constraint spells it — 'rest_claim', not 'RestClaim'.</summary>
    public static string FromKind(SessionKind kind) => kind switch
    {
        SessionKind.Lift => "lift",
        SessionKind.Run => "run",
        SessionKind.RestClaim => "rest_claim",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown session kind."),
    };

    public static SessionKind ToKind(string kind) => kind switch
    {
        "lift" => SessionKind.Lift,
        "run" => SessionKind.Run,
        "rest_claim" => SessionKind.RestClaim,
        _ => throw new InvalidOperationException($"Stored session kind '{kind}' is not one the app knows."),
    };

    public static SessionRow ToRow(LoggedSession session, long updatedAt, string deviceId, bool deleted = false) => new(
        SqliteValues.FromGuid(session.Id),
        SqliteValues.FromGuid(session.UserId),
        SqliteValues.FromDate(session.Date),
        FromKind(session.Kind),
        session.ProgramId,
        session.DayKey,
        session.IntervalWeek,
        session.IntervalDay,
        session.DurationSeconds,
        session.Notes,
        updatedAt,
        deviceId,
        SqliteValues.FromBool(deleted));

    public static LoggedSession ToDomain(SessionRow row, IReadOnlyList<SetEntry> sets) => new(
        SqliteValues.ToGuid(row.Id),
        SqliteValues.ToGuid(row.UserId),
        SqliteValues.ToDate(row.Date),
        ToKind(row.Kind),
        row.ProgramId,
        row.DayKey,
        (int?)row.IntervalWeek,
        (int?)row.IntervalDay,
        (int?)row.DurationS,
        row.Notes,
        sets);

    public static SetRow ToRow(SetEntry set) => new(
        SqliteValues.FromGuid(set.Id),
        SqliteValues.FromGuid(set.WorkoutId),
        set.ExerciseId,
        set.SetOrder,
        SqliteValues.FromDecimal(set.WeightKg),
        set.Reps,
        SqliteValues.FromEnum(set.Mode),
        SqliteValues.FromDecimal(set.EntryWeight),
        SqliteValues.FromEnum(set.EntryUnit),
        set.EquipmentId,
        SqliteValues.FromBool(set.IsWarmup),
        set.Notes);

    public static SetEntry ToDomain(SetRow row) => new(
        SqliteValues.ToGuid(row.Id),
        SqliteValues.ToGuid(row.SessionId),
        row.ExerciseId,
        (int)row.SetOrder,
        SqliteValues.ToDecimal(row.WeightKg),
        (int?)row.Reps,
        SqliteValues.ToEnum<EntryMode>(row.EntryMode),
        SqliteValues.ToDecimal(row.EntryWeight),
        SqliteValues.ToEnum<WeightUnit>(row.EntryUnit),
        row.EquipmentId,
        SqliteValues.ToBool(row.IsWarmup),
        row.Notes);
}
