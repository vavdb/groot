using Groot.Core.Health;

namespace Groot.Core.Tests;

public class RouteTrackTests
{
    // Amsterdam, where a degree of longitude is about 0.66 of a degree of latitude on the ground.
    private const double Lat = 52.37;
    private const double Lon = 4.90;

    // About 11.1 m of latitude, comfortably past the jitter filter.
    private const double Step = 0.0001;

    private static RouteFix Fix(int second, double lat, double lon, double accuracy = 8, int? bpm = null) =>
        new(second, lat, lon, accuracy, bpm);

    [Fact]
    public void An_empty_track_has_nothing_to_draw()
    {
        var view = new RouteTrack().View();

        Assert.True(view.IsEmpty);
        Assert.Equal(0, view.DistanceMetres);
        Assert.Equal(1, view.AspectRatio);
    }

    [Fact]
    public void The_first_fix_is_always_kept()
    {
        var track = new RouteTrack();

        Assert.True(track.Add(Fix(0, Lat, Lon)));
        Assert.Single(track.Fixes);
        Assert.Equal(0, track.DistanceMetres);
    }

    [Fact]
    public void Distance_sums_the_ground_between_kept_fixes()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(5, Lat + Step, Lon));
        track.Add(Fix(10, Lat + Step * 2, Lon));

        // Two steps of about 11.1 m each.
        Assert.InRange(track.DistanceMetres, 21, 23);
    }

    [Theory]
    [InlineData(41)]     // past the accuracy limit
    [InlineData(0)]      // a device reporting no accuracy at all
    [InlineData(-1)]
    public void A_fix_the_device_is_not_confident_about_is_dropped(double accuracy)
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));

        Assert.False(track.Add(Fix(5, Lat + Step, Lon, accuracy)));
        Assert.Single(track.Fixes);
    }

    [Fact]
    public void Null_island_is_dropped()
    {
        Assert.False(new RouteTrack().Add(Fix(0, 0, 0)));
    }

    [Fact]
    public void A_fix_that_has_not_moved_far_enough_is_jitter_and_is_dropped()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));

        // About 1.1 m: a phone standing still.
        Assert.False(track.Add(Fix(5, Lat + 0.00001, Lon)));
        Assert.Single(track.Fixes);
        Assert.Equal(0, track.DistanceMetres);
    }

    [Fact]
    public void Standing_still_for_a_whole_warmup_adds_no_distance()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));

        for (var second = 1; second <= 300; second++)
        {
            // Wander a metre either way, the way a stationary phone reports.
            var wobble = (second % 2 == 0 ? 1 : -1) * 0.000009;
            track.Add(Fix(second, Lat + wobble, Lon + wobble));
        }

        Assert.Equal(0, track.DistanceMetres);
        Assert.Single(track.Fixes);
    }

    [Fact]
    public void A_fix_that_arrives_out_of_order_is_dropped()
    {
        var track = new RouteTrack();
        track.Add(Fix(10, Lat, Lon));

        Assert.False(track.Add(Fix(5, Lat + Step, Lon)));
        Assert.Single(track.Fixes);
    }

    [Fact]
    public void A_long_silence_marks_the_join_as_a_gap()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(5, Lat + Step, Lon));
        track.Add(Fix(5 + RouteTrack.GapSeconds + 1, Lat + Step * 2, Lon));

        var points = track.View().Points;
        Assert.False(points[0].GapBefore);
        Assert.False(points[1].GapBefore);
        Assert.True(points[2].GapBefore);
    }

    [Fact]
    public void A_silence_inside_the_limit_is_not_a_gap()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(RouteTrack.GapSeconds, Lat + Step, Lon));

        Assert.False(track.View().Points[1].GapBefore);
    }

    [Fact]
    public void Every_point_lands_inside_the_unit_square()
    {
        var track = new RouteTrack();
        for (var i = 0; i < 40; i++)
        {
            var angle = i / 40.0 * Math.PI * 2;
            track.Add(Fix(i * 5, Lat + Math.Sin(angle) * 0.004, Lon + Math.Cos(angle) * 0.004));
        }

        Assert.All(track.View().Points, p =>
        {
            Assert.InRange(p.X, 0, 1);
            Assert.InRange(p.Y, 0, 1);
        });
    }

    [Fact]
    public void North_is_up()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));                 // south
        track.Add(Fix(5, Lat + Step * 10, Lon));     // north

        var points = track.View().Points;
        Assert.True(points[1].Y < points[0].Y, "the northern point should sit higher up the box");
    }

    [Fact]
    public void East_is_right()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(5, Lat, Lon + Step * 10));

        var points = track.View().Points;
        Assert.True(points[1].X > points[0].X, "the eastern point should sit further right");
    }

    [Fact]
    public void A_wide_route_reports_a_wide_aspect_ratio()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(5, Lat + Step * 10, Lon));        // 1 unit of latitude
        track.Add(Fix(10, Lat, Lon + Step * 30));       // 3 units of longitude, before the cosine

        // 30 units of longitude at 52 degrees north is about 0.61 of that on the ground.
        Assert.InRange(track.View().AspectRatio, 1.5, 2.2);
    }

    [Fact]
    public void A_route_that_never_moved_east_still_draws()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(5, Lat + Step * 10, Lon));

        var view = track.View();
        Assert.Equal(1, view.AspectRatio);
        Assert.All(view.Points, p => Assert.InRange(p.X, 0, 1));
    }

    [Fact]
    public void The_heart_rate_at_each_point_is_carried_through()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon, bpm: 96));
        track.Add(Fix(5, Lat + Step, Lon, bpm: 148));

        var points = track.View().Points;
        Assert.Equal(96, points[0].Bpm);
        Assert.Equal(148, points[1].Bpm);
    }

    [Fact]
    public void Clear_returns_the_track_to_empty_and_it_stays_usable()
    {
        var track = new RouteTrack();
        track.Add(Fix(0, Lat, Lon));
        track.Add(Fix(5, Lat + Step, Lon));
        track.Clear();

        Assert.True(track.View().IsEmpty);
        Assert.Equal(0, track.DistanceMetres);
        Assert.Null(track.Last);

        Assert.True(track.Add(Fix(0, Lat, Lon)));
        Assert.Single(track.Fixes);
    }

    [Fact]
    public void A_known_distance_comes_out_right()
    {
        // One degree of latitude is about 111.2 km anywhere on Earth.
        var metres = Geo.DistanceMetres(52.0, 4.9, 53.0, 4.9);

        Assert.InRange(metres, 111_000, 111_400);
    }

    [Fact]
    public void The_same_point_is_zero_metres_away_from_itself()
    {
        Assert.Equal(0, Geo.DistanceMetres(Lat, Lon, Lat, Lon), 6);
    }
}
