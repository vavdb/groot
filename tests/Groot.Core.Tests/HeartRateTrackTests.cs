using Groot.Core.Health;

namespace Groot.Core.Tests;

public class HeartRateTrackTests
{
    // A 20 minute session across 40 buckets: one bucket is 30 seconds.
    private const int TotalSeconds = 1200;
    private const int Buckets = 40;

    private static HeartRateTrack Track() => new(TotalSeconds, Buckets);

    [Fact]
    public void Empty_track_draws_an_empty_view()
    {
        var view = Track().View();

        Assert.True(view.IsEmpty);
        Assert.Equal(0, view.SampleCount);
        Assert.Equal(0, view.FilledBuckets);
        Assert.Null(view.Current);
        Assert.Null(view.Average);
        Assert.Equal(Buckets, view.Buckets.Count);
        Assert.All(view.Buckets, Assert.Null);
    }

    [Fact]
    public void First_reading_fills_the_first_bucket_only()
    {
        var track = Track();
        track.Add(0, 96);

        var view = track.View();
        Assert.Equal(1, view.FilledBuckets);
        Assert.Equal(96, view.Buckets[0]);
        Assert.Null(view.Buckets[1]);
        Assert.Equal(96, view.Current);
    }

    [Fact]
    public void Readings_land_in_the_bucket_their_elapsed_time_falls_in()
    {
        var track = Track();
        track.Add(0, 80);      // bucket 0
        track.Add(600, 150);   // halfway: bucket 20

        var view = track.View();
        Assert.Equal(80, view.Buckets[0]);
        Assert.Equal(150, view.Buckets[20]);
        Assert.Equal(21, view.FilledBuckets);
    }

    [Fact]
    public void A_gap_between_readings_carries_the_previous_value_forward()
    {
        var track = Track();
        track.Add(0, 80);
        track.Add(120, 130);   // bucket 4, three buckets skipped

        var view = track.View();
        Assert.Equal(80, view.Buckets[1]);
        Assert.Equal(80, view.Buckets[2]);
        Assert.Equal(80, view.Buckets[3]);
        Assert.Equal(130, view.Buckets[4]);
    }

    [Fact]
    public void Buckets_before_the_first_reading_stay_empty()
    {
        var track = Track();
        track.Add(300, 140);   // bucket 10

        var view = track.View();
        Assert.Null(view.Buckets[0]);
        Assert.Null(view.Buckets[9]);
        Assert.Equal(140, view.Buckets[10]);
        Assert.Equal(11, view.FilledBuckets);
    }

    [Fact]
    public void Several_readings_in_one_bucket_are_averaged()
    {
        var track = Track();
        track.Add(0, 100);
        track.Add(10, 110);
        track.Add(20, 120);    // all inside bucket 0

        Assert.Equal(110, track.View().Buckets[0]);
    }

    [Fact]
    public void The_last_second_lands_in_the_last_bucket()
    {
        var track = Track();
        track.Add(TotalSeconds, 165);

        var view = track.View();
        Assert.Equal(165, view.Buckets[Buckets - 1]);
        Assert.Equal(Buckets, view.FilledBuckets);
    }

    [Fact]
    public void Readings_past_the_end_of_the_session_are_dropped()
    {
        var track = Track();
        track.Add(0, 90);

        Assert.False(track.Add(TotalSeconds + 1, 170));
        Assert.Equal(1, track.SampleCount);
        Assert.Equal(90, track.Current);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(251)]
    [InlineData(600)]
    public void Implausible_readings_are_dropped_not_clamped(int bpm)
    {
        var track = Track();
        track.Add(60, 120);

        Assert.False(track.Add(120, bpm));
        Assert.Equal(1, track.SampleCount);
        Assert.Equal(120, track.Current);
    }

    [Fact]
    public void A_negative_elapsed_time_is_dropped()
    {
        Assert.False(Track().Add(-1, 120));
    }

    [Fact]
    public void Summary_readings_cover_every_sample_not_every_bucket()
    {
        var track = Track();
        track.Add(0, 100);
        track.Add(5, 140);     // same bucket as the first
        track.Add(600, 120);

        var view = track.View();
        Assert.Equal(3, view.SampleCount);
        Assert.Equal(100, view.Minimum);
        Assert.Equal(140, view.Maximum);
        Assert.Equal(120, view.Average);
        Assert.Equal(120, view.Current);
    }

    [Fact]
    public void Current_is_the_last_reading_recorded_even_when_it_is_lower()
    {
        var track = Track();
        track.Add(0, 150);
        track.Add(60, 95);

        Assert.Equal(95, track.Current);
        Assert.Equal(150, track.View().Maximum);
    }

    [Fact]
    public void Clear_returns_the_track_to_empty()
    {
        var track = Track();
        track.Add(0, 100);
        track.Add(600, 160);
        track.Clear();

        var view = track.View();
        Assert.True(view.IsEmpty);
        Assert.Equal(0, view.FilledBuckets);
        Assert.Null(track.Current);
        Assert.All(view.Buckets, Assert.Null);
    }

    [Fact]
    public void A_track_is_reusable_after_clear()
    {
        var track = Track();
        track.Add(0, 100);
        track.Clear();
        track.Add(0, 130);

        var view = track.View();
        Assert.Equal(1, view.SampleCount);
        Assert.Equal(130, view.Buckets[0]);
        Assert.Equal(130, view.Minimum);
        Assert.Equal(130, view.Maximum);
    }

    [Fact]
    public void A_session_needs_a_length_and_at_least_one_bucket()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeartRateTrack(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeartRateTrack(600, 0));
    }

    [Fact]
    public void A_short_session_still_buckets_without_dividing_by_zero()
    {
        var track = new HeartRateTrack(totalSeconds: 5, buckets: 40);
        track.Add(0, 80);
        track.Add(5, 120);

        var view = track.View();
        Assert.Equal(80, view.Buckets[0]);
        Assert.Equal(120, view.Buckets[Buckets - 1]);
    }
}
