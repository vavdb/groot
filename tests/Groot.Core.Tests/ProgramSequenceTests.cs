using Groot.Core.Programs;

namespace Groot.Core.Tests;

/// <summary>
/// What a program says comes next. The rotation is the program's own business, not the
/// calendar's: GZCLP runs three sessions a week through a four-day rotation, so the two drift
/// apart on purpose and "next" can only be answered from the last day trained.
/// </summary>
public sealed class ProgramSequenceTests
{
    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");
    private static readonly IntervalProgram Couch = ProgramCatalog.Embedded.IntervalProgram("0-to-5k");

    [Theory]
    [InlineData("A1", "B1")]
    [InlineData("B1", "A2")]
    [InlineData("A2", "B2")]
    [InlineData("B2", "A1")]
    public void The_rotation_advances_and_wraps(string day, string expected) =>
        Assert.Equal(expected, Gzclp.NextDayAfter(day));

    [Fact]
    public void A_program_starts_at_the_first_day_of_its_rotation() =>
        Assert.Equal("A1", Gzclp.FirstDay);

    [Fact]
    public void A_day_the_program_does_not_have_is_rejected_rather_than_guessed() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Gzclp.NextDayAfter("C3"));

    [Fact]
    public void The_next_run_is_the_next_session_of_the_same_week() =>
        Assert.Equal(new IntervalSession(1, 2), Couch.NextAfter(new IntervalSession(1, 1)));

    [Fact]
    public void The_last_session_of_a_week_rolls_into_the_first_of_the_next()
    {
        var lastOfWeekOne = Couch.Week(1).DayNumbers[^1];

        Assert.Equal(new IntervalSession(2, 1), Couch.NextAfter(new IntervalSession(1, lastOfWeekOne)));
    }

    [Fact]
    public void The_final_session_of_the_program_has_nothing_after_it()
    {
        var lastWeek = Couch.WeekNumbers[^1];
        var lastDay = Couch.Week(lastWeek).DayNumbers[^1];

        Assert.Null(Couch.NextAfter(new IntervalSession(lastWeek, lastDay)));
    }

    [Fact]
    public void An_interval_program_starts_at_week_one_session_one() =>
        Assert.Equal(new IntervalSession(1, 1), Couch.FirstSession);

    [Fact]
    public void A_session_the_week_does_not_have_is_rejected_rather_than_guessed() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Couch.NextAfter(new IntervalSession(1, 99)));
}
