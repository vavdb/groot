using Groot.Core.Health;
using Groot.Core.Sessions;

namespace Groot.Data.Tests;

/// <summary>
/// Heart rate readings and route fixes survive a round trip through the file, and a session's
/// measurements go when the session does.
/// </summary>
public sealed class SessionMetricsStoreTests
{
    private const double Lat = 52.37;
    private const double Lon = 4.90;

    private static async Task<(TemporaryDatabase Temp, Guid SessionId)> WithRun()
    {
        var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var sessionId = Guid.NewGuid();

        await temp.Sessions.Save(
            LoggedSession.Run(sessionId, userId, new DateOnly(2026, 8, 31), "0-to-5k", week: 1, day: 2, durationSeconds: 1800),
            updatedAt: 1,
            deviceId: "test");

        return (temp, sessionId);
    }

    [Fact]
    public async Task Heart_rate_readings_come_back_as_they_went_in()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(0, 96), new(5, 104), new(10, 131)]);

        var readings = await temp.Metrics.ReadHeartRate(sessionId, "amazfit");

        Assert.Equal(3, readings.Count);
        Assert.Equal(new HeartRateSample(0, 96), readings[0]);
        Assert.Equal(new HeartRateSample(10, 131), readings[2]);
    }

    [Fact]
    public async Task Two_monitors_keep_their_own_readings()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(0, 96)]);
        await temp.Metrics.SaveHeartRate(sessionId, "fitbit-air", [new(0, 99)]);

        Assert.Equal(96, (await temp.Metrics.ReadHeartRate(sessionId, "amazfit"))[0].Bpm);
        Assert.Equal(99, (await temp.Metrics.ReadHeartRate(sessionId, "fitbit-air"))[0].Bpm);
        Assert.Equal(["amazfit", "fitbit-air"], await temp.Metrics.ReadHeartRateSources(sessionId));
    }

    [Fact]
    public async Task Saving_one_monitor_again_replaces_only_its_own_readings()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(0, 96), new(5, 104)]);
        await temp.Metrics.SaveHeartRate(sessionId, "fitbit-air", [new(0, 99)]);
        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(0, 120)]);

        var amazfit = await temp.Metrics.ReadHeartRate(sessionId, "amazfit");
        Assert.Single(amazfit);
        Assert.Equal(120, amazfit[0].Bpm);
        Assert.Single(await temp.Metrics.ReadHeartRate(sessionId, "fitbit-air"));
    }

    [Fact]
    public async Task Two_readings_in_one_second_keep_the_later_one_rather_than_failing()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(4, 100), new(4, 108)]);

        var readings = await temp.Metrics.ReadHeartRate(sessionId, "amazfit");
        Assert.Single(readings);
        Assert.Equal(108, readings[0].Bpm);
    }

    [Fact]
    public async Task An_implausible_reading_never_reaches_the_file()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(0, 96), new(5, 900), new(10, 104)]);

        var readings = await temp.Metrics.ReadHeartRate(sessionId, "amazfit");
        Assert.Equal(2, readings.Count);
        Assert.DoesNotContain(readings, r => r.Bpm == 900);
    }

    [Fact]
    public async Task A_route_comes_back_with_its_positions_intact()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveRoute(sessionId,
        [
            new(0, Lat, Lon, 8, 96),
            new(5, Lat + 0.0001, Lon + 0.0002, 6.5, 131),
        ]);

        var fixes = await temp.Metrics.ReadRoute(sessionId);

        Assert.Equal(2, fixes.Count);
        // Ten-millionths of a degree is about a centimetre, so seven places round-trip exactly.
        Assert.Equal(Lat, fixes[0].Latitude, 7);
        Assert.Equal(Lon, fixes[0].Longitude, 7);
        Assert.Equal(Lat + 0.0001, fixes[1].Latitude, 7);
        Assert.Equal(Lon + 0.0002, fixes[1].Longitude, 7);
        Assert.Equal(6.5, fixes[1].AccuracyMetres, 2);
        Assert.Equal(131, fixes[1].Bpm);
    }

    [Fact]
    public async Task A_route_fix_without_a_heart_rate_stores_as_one()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveRoute(sessionId, [new(0, Lat, Lon, 8)]);

        Assert.Null((await temp.Metrics.ReadRoute(sessionId))[0].Bpm);
    }

    [Fact]
    public async Task An_unusable_fix_never_reaches_the_file()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveRoute(sessionId,
        [
            new(0, Lat, Lon, 8),
            new(5, Lat, Lon, 200),   // the device is not confident
            new(10, 0, 0, 5),        // null island
        ]);

        Assert.Single(await temp.Metrics.ReadRoute(sessionId));
    }

    [Fact]
    public async Task Saving_a_route_again_replaces_it()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveRoute(sessionId, [new(0, Lat, Lon, 8), new(5, Lat + 0.0001, Lon, 8)]);
        await temp.Metrics.SaveRoute(sessionId, [new(0, Lat, Lon, 8)]);

        Assert.Single(await temp.Metrics.ReadRoute(sessionId));
    }

    [Fact]
    public async Task Measurements_cannot_exist_without_their_session()
    {
        using var temp = new TemporaryDatabase();

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => temp.Metrics.SaveHeartRate(Guid.NewGuid(), "amazfit", [new(0, 96)]));

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => temp.Metrics.SaveRoute(Guid.NewGuid(), [new(0, Lat, Lon, 8)]));
    }

    [Fact]
    public async Task Reopening_the_file_finds_the_measurements_again()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        await temp.Metrics.SaveHeartRate(sessionId, "amazfit", [new(0, 96)]);
        await temp.Metrics.SaveRoute(sessionId, [new(0, Lat, Lon, 8, 96)]);

        var reopened = new Groot.Data.Sessions.SessionMetricsStore(temp.Reopen());

        Assert.Single(await reopened.ReadHeartRate(sessionId, "amazfit"));
        Assert.Single(await reopened.ReadRoute(sessionId));
    }

    [Fact]
    public async Task Nothing_recorded_reads_back_as_nothing()
    {
        var (temp, sessionId) = await WithRun();
        using var _ = temp;

        Assert.Empty(await temp.Metrics.ReadHeartRate(sessionId, "amazfit"));
        Assert.Empty(await temp.Metrics.ReadRoute(sessionId));
        Assert.Empty(await temp.Metrics.ReadHeartRateSources(sessionId));
    }
}
