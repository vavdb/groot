namespace Groot.Core.Contract;

public enum SessionKind { Lift, Run, RestClaim }

/// <summary>A logged session, reduced to what the contract cares about.</summary>
public sealed record ContractSession(DateOnly Date, SessionKind Kind);

/// <summary>The weekly habit contract: minimum counts that keep the streak.</summary>
public sealed record WeekContract(int Lifts = 2, int Runs = 2, int RestDays = 1, int Jokers = 2);

public sealed record WeekEvaluation(
    DateOnly WeekStart,
    int LiftCredits,
    int RunCredits,
    bool RestKept,
    int JokersSpent,
    bool ContractMet,
    bool Overgrowth);

/// <summary>
/// Evaluates one week against the contract. Pure and deterministic.
/// Rules: a session credits its kind once per calendar day; rest is satisfied by any
/// session-free day; jokers fill missing activity credits (never rest); training all 7 days
/// keeps the week but flags overgrowth.
/// </summary>
public static class ContractEvaluator
{
    public static WeekEvaluation Evaluate(
        DateOnly weekStart,
        IReadOnlyCollection<ContractSession> sessions,
        WeekContract contract)
    {
        var weekDays = Enumerable.Range(0, 7).Select(weekStart.AddDays).ToArray();
        var inWeek = sessions.Where(s => weekDays.Contains(s.Date)).ToArray();

        var liftCredits = inWeek.Where(s => s.Kind == SessionKind.Lift).Select(s => s.Date).Distinct().Count();
        var runCredits = inWeek.Where(s => s.Kind == SessionKind.Run).Select(s => s.Date).Distinct().Count();

        var activeDays = inWeek
            .Where(s => s.Kind is SessionKind.Lift or SessionKind.Run)
            .Select(s => s.Date).Distinct().Count();
        var restKept = activeDays < 7;
        var overgrowth = !restKept;

        var missing = Math.Max(0, contract.Lifts - liftCredits) + Math.Max(0, contract.Runs - runCredits);
        var jokersSpent = Math.Min(missing, contract.Jokers);
        var contractMet = missing <= contract.Jokers;

        return new WeekEvaluation(weekStart, liftCredits, runCredits, restKept, jokersSpent, contractMet, overgrowth);
    }

    /// <summary>Consecutive kept weeks counted backwards from <paramref name="latestWeekStart"/>.</summary>
    public static int StreakWeeks(IReadOnlyList<WeekEvaluation> history, DateOnly latestWeekStart)
    {
        var byStart = history.ToDictionary(w => w.WeekStart);
        var streak = 0;
        for (var start = latestWeekStart; byStart.TryGetValue(start, out var week) && week.ContractMet; start = start.AddDays(-7))
            streak++;
        return streak;
    }

    /// <summary>Start of the week containing <paramref name="date"/> for a given first day of week.</summary>
    public static DateOnly WeekStartOf(DateOnly date, DayOfWeek firstDay)
    {
        var diff = ((int)date.DayOfWeek - (int)firstDay + 7) % 7;
        return date.AddDays(-diff);
    }
}
