namespace Groot.Core.Health;

/// <summary>
/// Reads the Bluetooth Heart Rate Measurement characteristic, 0x2A37.
/// <para>
/// The layout is a flags byte and then the rate, and the flags decide how wide the rate is and
/// what follows it. Every device that broadcasts a heart rate at all speaks this, from a chest
/// strap to a watch, so this is the whole vocabulary needed to read any of them.
/// </para>
/// <para>
/// Lives in Core rather than beside the Android client because it is pure byte work: the tests
/// are the specification, and they run on every platform including the ones with no Bluetooth.
/// </para>
/// </summary>
public static class HeartRateMeasurement
{
    // Flags byte, per the Bluetooth SIG Heart Rate Service specification.
    private const int WideValueFlag = 1 << 0;      // 0: the rate is one byte. 1: two, little endian.
    private const int ContactStatusMask = 0b110;   // Whether the sensor knows it is touching skin.
    private const int ContactSupportedNotDetected = 0b100;

    /// <summary>
    /// The heart rate in a notification, or null if there is not a usable one in it.
    /// <para>
    /// Null covers four cases, and all of them are ordinary rather than exceptional: the packet is
    /// too short to hold what its own flags promise, the device says its sensor is not touching
    /// skin, the rate is zero, or the rate is outside what a heart does. A watch pushed up the arm
    /// reports the middle two constantly, and treating those as readings puts a flat line through
    /// the trace that never happened.
    /// </para>
    /// </summary>
    public static int? Parse(byte[]? value)
    {
        if (value is null || value.Length < 2) return null;

        var flags = value[0];

        // The device is telling us it cannot feel skin. Whatever number follows is not a heart
        // rate. The other three combinations all mean "take the reading": two of them say the
        // device has no contact sensor to ask.
        if ((flags & ContactStatusMask) == ContactSupportedNotDetected) return null;

        var wide = (flags & WideValueFlag) != 0;
        if (wide && value.Length < 3) return null;

        var bpm = wide ? value[1] | (value[2] << 8) : value[1];

        return bpm is >= HeartRateSample.MinimumPlausibleBpm and <= HeartRateSample.MaximumPlausibleBpm
            ? bpm
            : null;
    }
}
