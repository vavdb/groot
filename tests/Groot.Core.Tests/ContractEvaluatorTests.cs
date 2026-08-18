using Groot.Core.Contract;

namespace Groot.Core.Tests;

public class ContractEvaluatorTests
{
    private static readonly WeekContract Contract = new();
    private static readonly DateOnly Monday = new(2026, 8, 17);

    private static ContractSession Lift(int dayOffset) => new(Monday.AddDays(dayOffset), SessionKind.Lift);
    private static ContractSession Run(int dayOffset) => new(Monday.AddDays(dayOffset), SessionKind.Run);

    [Fact]
    public void Full_template_week_meets_contract_without_jokers()
    {
        // Mon run, Tue lift, Wed run, Thu lift, Fri run, Sat lift, Sun rest
        var sessions = new[] { Run(0), Lift(1), Run(2), Lift(3), Run(4), Lift(5) };
        var eval = ContractEvaluator.Evaluate(Monday, sessions, Contract);

        Assert.True(eval.ContractMet);
        Assert.Equal(0, eval.JokersSpent);
        Assert.True(eval.RestKept);
        Assert.False(eval.Overgrowth);
    }

    [Fact]
    public void One_missing_lift_costs_one_joker()
    {
        var sessions = new[] { Run(0), Lift(1), Run(2) };
        var eval = ContractEvaluator.Evaluate(Monday, sessions, Contract);

        Assert.True(eval.ContractMet);
        Assert.Equal(1, eval.JokersSpent);
    }

    [Fact]
    public void Three_missing_credits_break_the_week()
    {
        var sessions = new[] { Run(0) };
        var eval = ContractEvaluator.Evaluate(Monday, sessions, Contract);

        Assert.False(eval.ContractMet);
    }

    [Fact]
    public void Two_lifts_same_day_credit_once()
    {
        var sessions = new[] { Lift(1), Lift(1), Run(0), Run(2) };
        var eval = ContractEvaluator.Evaluate(Monday, sessions, Contract);

        Assert.Equal(1, eval.LiftCredits);
        Assert.Equal(1, eval.JokersSpent);
    }

    [Fact]
    public void Training_all_seven_days_keeps_week_but_flags_overgrowth()
    {
        var sessions = Enumerable.Range(0, 7)
            .SelectMany(d => new[] { Lift(d), Run(d) })
            .ToArray();
        var eval = ContractEvaluator.Evaluate(Monday, sessions, Contract);

        Assert.True(eval.ContractMet);
        Assert.False(eval.RestKept);
        Assert.True(eval.Overgrowth);
    }

    [Fact]
    public void Streak_counts_consecutive_kept_weeks_backwards()
    {
        var history = new[]
        {
            new WeekEvaluation(Monday.AddDays(-21), 2, 2, true, 0, ContractMet: true, false),
            new WeekEvaluation(Monday.AddDays(-14), 2, 2, true, 0, ContractMet: false, false),
            new WeekEvaluation(Monday.AddDays(-7), 2, 2, true, 1, ContractMet: true, false),
            new WeekEvaluation(Monday, 2, 2, true, 0, ContractMet: true, false),
        };

        Assert.Equal(2, ContractEvaluator.StreakWeeks(history, Monday));
    }

    [Fact]
    public void Week_start_respects_first_day_setting()
    {
        var wednesday = new DateOnly(2026, 8, 19);
        Assert.Equal(new DateOnly(2026, 8, 17), ContractEvaluator.WeekStartOf(wednesday, DayOfWeek.Monday));
        Assert.Equal(new DateOnly(2026, 8, 16), ContractEvaluator.WeekStartOf(wednesday, DayOfWeek.Sunday));
    }
}
