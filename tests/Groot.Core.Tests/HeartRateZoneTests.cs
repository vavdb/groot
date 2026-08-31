using Groot.Core.Health;

namespace Groot.Core.Tests;

public class HeartRateZoneTests
{
    [Theory]
    [InlineData(90, HeartRateZone.Easy)]        // 47%
    [InlineData(113, HeartRateZone.Easy)]       // 59%
    [InlineData(114, HeartRateZone.Steady)]     // 60%
    [InlineData(133, HeartRateZone.Moderate)]   // 70%
    [InlineData(152, HeartRateZone.Hard)]       // 80%
    [InlineData(171, HeartRateZone.Maximum)]    // 90%
    [InlineData(195, HeartRateZone.Maximum)]    // over the stated maximum
    public void A_reading_falls_in_the_zone_its_share_of_maximum_puts_it_in(int bpm, HeartRateZone expected)
    {
        Assert.Equal(expected, HeartRateZones.Of(bpm));
    }

    [Fact]
    public void A_higher_maximum_moves_every_boundary_up()
    {
        Assert.Equal(HeartRateZone.Hard, HeartRateZones.Of(160));
        Assert.Equal(HeartRateZone.Moderate, HeartRateZones.Of(160, maximumBpm: 210));
    }

    [Fact]
    public void A_maximum_that_is_not_a_heart_rate_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HeartRateZones.Of(120, maximumBpm: 0));
    }
}
