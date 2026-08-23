using Groot.Core.Contract;
using Groot.Core.Programs;

namespace Groot.Data.Sessions;

/// <summary>
/// Answers "where was I?" for a program by reading what was actually logged. The program owns
/// what follows a given day; this only supplies the day it follows, so a screen opening on
/// Wednesday resumes the rotation instead of restarting it.
/// </summary>
public sealed class ProgramProgress(SessionStore sessions)
{
    /// <summary>
    /// The lifting day to load next: the day after the last one logged, or the first day of the
    /// rotation when the program has never been trained.
    /// </summary>
    public async Task<string> NextLiftDay(Guid userId, LiftProgram program, CancellationToken cancellationToken = default)
    {
        var latest = await sessions.LatestOfKind(userId, program.Id, SessionKind.Lift, cancellationToken);

        // A day the program no longer rotates through restarts the rotation. The program's own
        // version can move under stored history, and a screen that cannot open is worse than one
        // that opens on A1.
        return latest?.DayKey is { } lastDay && program.Rotates(lastDay)
            ? program.NextDayAfter(lastDay)
            : program.FirstDay;
    }

    /// <summary>
    /// The run to load next: the session after the last one logged, or the program's first.
    /// Null once every session has been run.
    /// </summary>
    public async Task<IntervalSession?> NextRun(Guid userId, IntervalProgram program, CancellationToken cancellationToken = default)
    {
        var latest = await sessions.LatestOfKind(userId, program.Id, SessionKind.Run, cancellationToken);

        if (latest is not { IntervalWeek: { } week, IntervalDay: { } day })
            return program.FirstSession;

        // Same reasoning as the rotation: a session the program no longer has starts it over
        // rather than throwing on the way to the screen.
        var logged = new IntervalSession(week, day);
        return program.Has(logged) ? program.NextAfter(logged) : program.FirstSession;
    }
}
