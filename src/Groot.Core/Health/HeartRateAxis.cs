namespace Groot.Core.Health;

/// <summary>
/// The vertical range a trace is drawn against. Derived from the readings so a session that
/// never leaves the 70s still fills the box, with a floor on the span so a resting heart rate
/// does not turn a two-beat wobble into a mountain range.
/// </summary>
/// <param name="Low">Bottom of the drawn range, in bpm.</param>
/// <param name="High">Top of the drawn range, in bpm.</param>
public sealed record HeartRateAxis(int Low, int High)
{
    /// <summary>The narrowest range we will draw. Under this, small changes read as large ones.</summary>
    public const int MinimumSpan = 40;

    /// <summary>Range in bpm, always at least <see cref="MinimumSpan"/>.</summary>
    public int Span => High - Low;

    /// <summary>Where a reading sits in the range, 0 at <see cref="Low"/> and 1 at <see cref="High"/>.</summary>
    public double Fraction(int bpm) => Math.Clamp((bpm - Low) / (double)Span, 0, 1);

    /// <summary>
    /// The range for a set of readings: rounded out to the nearest 10 with 5 bpm of headroom,
    /// then widened around its centre if it came out narrower than <see cref="MinimumSpan"/>.
    /// </summary>
    public static HeartRateAxis For(int minimum, int maximum)
    {
        var low = (int)(Math.Floor((minimum - 5) / 10.0) * 10);
        var high = (int)(Math.Ceiling((maximum + 5) / 10.0) * 10);

        if (high - low < MinimumSpan)
        {
            var missing = MinimumSpan - (high - low);
            low -= missing / 2;
            high += missing - missing / 2;
        }

        // A heart rate never goes below zero, and the axis should not imply that it could.
        if (low < 0)
        {
            high -= low;
            low = 0;
        }

        return new HeartRateAxis(low, high);
    }

    /// <summary>The range to draw before any reading has arrived, so the empty box is the right height.</summary>
    public static HeartRateAxis Default { get; } = new(60, 180);
}
