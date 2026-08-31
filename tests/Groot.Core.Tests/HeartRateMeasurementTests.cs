using Groot.Core.Health;

namespace Groot.Core.Tests;

/// <summary>
/// The Bluetooth Heart Rate Measurement characteristic, byte for byte. These are the packets a
/// real watch sends; the flags byte is what decides how to read the rest.
/// </summary>
public class HeartRateMeasurementTests
{
    [Fact]
    public void An_eight_bit_rate_is_the_second_byte()
    {
        // flags 0x00: narrow value, no contact sensor claimed.
        Assert.Equal(72, HeartRateMeasurement.Parse([0x00, 72]));
    }

    [Fact]
    public void A_sixteen_bit_rate_is_read_low_byte_first()
    {
        // flags 0x01: wide value, 0x0096 little endian.
        Assert.Equal(150, HeartRateMeasurement.Parse([0x01, 0x96, 0x00]));
    }

    [Fact]
    public void A_wide_reading_beyond_a_heart_rate_is_refused_rather_than_truncated()
    {
        // 0x0102 = 258 bpm. Reading only the low byte would call this 2 and reading it wide and
        // clamping would call it 250; both are inventions. Nothing is a reading here.
        Assert.Null(HeartRateMeasurement.Parse([0x01, 0x02, 0x01]));
    }

    [Fact]
    public void A_wide_packet_that_is_too_short_for_its_own_flags_is_refused()
    {
        // The flags promise two bytes of rate and only one is present.
        Assert.Null(HeartRateMeasurement.Parse([0x01, 120]));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0x00 })]
    public void A_packet_with_no_room_for_a_rate_is_refused(byte[] value)
    {
        Assert.Null(HeartRateMeasurement.Parse(value));
    }

    [Fact]
    public void A_null_packet_is_refused()
    {
        Assert.Null(HeartRateMeasurement.Parse(null));
    }

    [Fact]
    public void A_sensor_that_says_it_is_not_touching_skin_produces_no_reading()
    {
        // flags 0b100: contact supported, contact not detected. This is a watch pushed up the
        // arm, and the number it sends is not a heart rate.
        Assert.Null(HeartRateMeasurement.Parse([0b100, 72]));
    }

    [Fact]
    public void A_sensor_that_says_it_is_touching_skin_produces_a_reading()
    {
        // flags 0b110: contact supported, contact detected.
        Assert.Equal(148, HeartRateMeasurement.Parse([0b110, 148]));
    }

    [Theory]
    [InlineData(0b000)] // no contact sensor
    [InlineData(0b010)] // no contact sensor, the other spelling of it
    public void A_device_with_no_contact_sensor_is_believed(int flags)
    {
        Assert.Equal(96, HeartRateMeasurement.Parse([(byte)flags, 96]));
    }

    [Fact]
    public void Energy_and_rr_intervals_after_the_rate_are_ignored()
    {
        // flags 0b11000: energy expended and RR intervals both present. Neither changes where
        // the rate is, and the run screen has no use for either.
        var packet = new byte[] { 0b11000, 132, 0x10, 0x27, 0x00, 0x03 };

        Assert.Equal(132, HeartRateMeasurement.Parse(packet));
    }

    [Fact]
    public void A_rate_of_zero_is_not_a_reading()
    {
        Assert.Null(HeartRateMeasurement.Parse([0x00, 0]));
    }

    [Theory]
    [InlineData(24)]   // below the plausible floor
    [InlineData(251)]  // above the ceiling
    public void A_rate_outside_what_a_heart_does_is_refused(byte bpm)
    {
        Assert.Null(HeartRateMeasurement.Parse([0x00, bpm]));
    }

    [Theory]
    [InlineData(25)]
    [InlineData(250)]
    public void The_ends_of_the_plausible_range_are_readings(byte bpm)
    {
        Assert.Equal(bpm, HeartRateMeasurement.Parse([0x00, bpm]));
    }

    [Fact]
    public void A_real_packet_from_a_watch_reads_as_the_rate_on_its_face()
    {
        // What an Amazfit sends mid-run: narrow value, contact detected.
        Assert.Equal(148, HeartRateMeasurement.Parse([0x06, 0x94]));
    }
}
