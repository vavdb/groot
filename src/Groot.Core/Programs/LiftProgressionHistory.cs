using Groot.Core.Contract;
using Groot.Core.Sessions;

namespace Groot.Core.Programs;

/// <summary>
/// Where every lift stands, replayed from what was actually logged. Nothing stores a working
/// weight: the sessions are the record, and folding them through
/// <see cref="LiftProgressionPlanner"/> reproduces the ladder. That keeps one answer on every
/// device — sessions pulled from another phone recompute rather than arriving with a stale
/// number — and lets a change to a program's progression rules take effect immediately.
/// <para>
/// The first session that trains a slot sets its starting weight, which needs no special case:
/// with no earlier state the lifter was on stage zero at whatever they lifted.
/// </para>
/// </summary>
public static class LiftProgressionHistory
{
    /// <summary>
    /// Folds a program's logged sessions, oldest first, into the state each slot is now in.
    /// Sessions belonging to other programs, other kinds, or exercises the program does not
    /// train are ignored.
    /// </summary>
    public static IReadOnlyDictionary<ExerciseSlot, ExerciseState> Replay(
        LiftProgram program,
        IReadOnlyList<LoggedSession> sessions)
    {
        var state = new Dictionary<ExerciseSlot, ExerciseState>();

        foreach (var session in sessions.Where(s => s.Kind == SessionKind.Lift && s.ProgramId == program.Id)
                                        .OrderBy(s => s.Date))
        {
            foreach (var slot in SlotsOf(program, session))
            {
                var performed = session.Sets
                    .Where(set => set.ExerciseId == slot.ExerciseId && !set.IsWarmup)
                    .OrderBy(set => set.SetOrder)
                    .ToArray();

                if (performed.Length == 0) continue;

                state[slot] = Advance(program, slot, performed, state.GetValueOrDefault(slot));
            }
        }

        return state;
    }

    /// <summary>
    /// The slots a session trained. The tier comes from the program's day, which is why a logged
    /// session records which day of the rotation it was.
    /// </summary>
    private static IReadOnlyList<ExerciseSlot> SlotsOf(LiftProgram program, LoggedSession session)
    {
        if (session.DayKey is not { Length: > 0 } dayKey) return [];

        var day = program.Days.FirstOrDefault(d => d.Key == dayKey);
        if (day is null) return [];

        return day.Exercises.Select(e => new ExerciseSlot(e.ExerciseId, e.Tier)).ToArray();
    }

    /// <summary>
    /// One session's effect on one slot. The weight comes from the heaviest working set rather
    /// than from what was planned, because a lifter who put 65 on the bar trained 65.
    /// </summary>
    private static ExerciseState Advance(
        LiftProgram program,
        ExerciseSlot slot,
        IReadOnlyList<SetEntry> performed,
        ExerciseState? previous)
    {
        var stage = previous?.Stage ?? 0;
        var weight = performed.Max(set => set.WeightKg);
        var scheme = program.TierFor(slot.Tier).SchemeAt(stage);

        // A set with no reps was never performed, so it cannot complete the scheme. The AMRAP is
        // the last set and only ever adds, so the target it has to clear is the scheme's.
        var completed = performed.Count >= scheme.Sets
            && performed.All(set => set.Reps >= scheme.Reps);

        // The AMRAP is the scheme's last set, not whatever was logged last. A lifter who stops
        // after one set of a 3x15+ has no AMRAP set, and reading their partial set as one lets a
        // session they abandoned add weight, because the T3 rule reads this and not completion.
        var amrapReps = scheme.AmrapLast && performed.Count >= scheme.Sets
            ? performed[scheme.Sets - 1].Reps
            : null;

        var outcome = new ExerciseOutcome(
            slot.ExerciseId,
            slot.Tier,
            weight,
            stage,
            completed,
            performed.Sum(set => set.Reps ?? 0),
            amrapReps);

        var next = LiftProgressionPlanner.Next(program, outcome);
        return new ExerciseState(next.WeightKg, next.Stage, weight);
    }
}
