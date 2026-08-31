using Groot.Core.Health;

namespace Groot.Core.Tests;

public class HeartRateAxisTests
{
    [Fact]
    public void A_wide_range_rounds_out_to_the_nearest_ten()
    {
        var axis = HeartRateAxis.For(72, 168);

        Assert.Equal(60, axis.Low);
        Assert.Equal(180, axis.High);
    }

    [Fact]
    public void A_narrow_range_widens_to_the_minimum_span()
    {
        var axis = HeartRateAxis.For(98, 102);

        Assert.Equal(HeartRateAxis.MinimumSpan, axis.Span);
        Assert.InRange(100, axis.Low, axis.High);
    }

    [Fact]
    public void One_reading_still_produces_a_drawable_range()
    {
        var axis = HeartRateAxis.For(120, 120);

        Assert.Equal(HeartRateAxis.MinimumSpan, axis.Span);
        Assert.InRange(120, axis.Low, axis.High);
    }

    [Fact]
    public void The_low_end_never_goes_below_zero()
    {
        // Only reachable below the plausible reading floor, but the axis is public and takes
        // any int: widening a near-zero range must not draw a negative heart rate.
        var axis = HeartRateAxis.For(0, 2);

        Assert.Equal(0, axis.Low);
        Assert.Equal(HeartRateAxis.MinimumSpan, axis.Span);
    }

    [Fact]
    public void The_lowest_plausible_reading_still_produces_a_positive_range()
    {
        var axis = HeartRateAxis.For(HeartRateSample.MinimumPlausibleBpm, 28);

        Assert.True(axis.Low >= 0);
        Assert.True(axis.Span >= HeartRateAxis.MinimumSpan);
        Assert.InRange(HeartRateSample.MinimumPlausibleBpm, axis.Low, axis.High);
    }

    [Theory]
    [InlineData(60, 0.0)]
    [InlineData(120, 0.5)]
    [InlineData(180, 1.0)]
    public void Fraction_maps_a_reading_onto_the_range(int bpm, double expected)
    {
        Assert.Equal(expected, new HeartRateAxis(60, 180).Fraction(bpm), 3);
    }

    [Fact]
    public void A_reading_outside_the_range_clamps_rather_than_drawing_off_the_box()
    {
        var axis = new HeartRateAxis(60, 180);

        Assert.Equal(0, axis.Fraction(40));
        Assert.Equal(1, axis.Fraction(220));
    }
}
