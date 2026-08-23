using Groot.Core.Contract;

namespace Groot.Core.Sessions;

/// <summary>
/// A session as it was performed and stored: the day it counts for, what kind of training it
/// was, where it sat in its program, and the sets it produced. The program fields are nullable
/// because they answer different questions per kind — a lift day is a rotation key ("A1"), a run
/// is a week and a session number, and a claimed rest day has neither.
/// </summary>
public sealed record LoggedSession(
    Guid Id,
    Guid UserId,
    DateOnly Date,
    SessionKind Kind,
    string? ProgramId,
    string? DayKey,
    int? IntervalWeek,
    int? IntervalDay,
    int? DurationSeconds,
    string? Notes,
    IReadOnlyList<SetEntry> Sets)
{
    /// <summary>A finished lifting day: which rotation day it was, and every set logged in it.</summary>
    public static LoggedSession Lift(
        Guid id,
        Guid userId,
        DateOnly date,
        string programId,
        string dayKey,
        IReadOnlyList<SetEntry> sets,
        int? durationSeconds = null,
        string? notes = null) =>
        new(id, userId, date, SessionKind.Lift, programId, dayKey, null, null, durationSeconds, notes, sets);

    /// <summary>A finished interval run: which week and which session of that week.</summary>
    public static LoggedSession Run(
        Guid id,
        Guid userId,
        DateOnly date,
        string programId,
        int week,
        int day,
        int? durationSeconds = null,
        string? notes = null) =>
        new(id, userId, date, SessionKind.Run, programId, null, week, day, durationSeconds, notes, []);

    /// <summary>A rest day the lifter claimed rather than one the calendar handed them.</summary>
    public static LoggedSession RestClaim(Guid id, Guid userId, DateOnly date, string? notes = null) =>
        new(id, userId, date, SessionKind.RestClaim, null, null, null, null, null, notes, []);

    /// <summary>This session reduced to what <see cref="ContractEvaluator"/> reads.</summary>
    public ContractSession ForContract => new(Date, Kind);
}

/// <summary>How many sessions were logged on one day. The season grid counts nothing itself.</summary>
public sealed record DailySessionCount(DateOnly Date, int Sessions);
