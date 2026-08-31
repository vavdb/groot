namespace Groot.UI.Health;

/// <summary>Where a measuring device is in its life, as far as a screen needs to care.</summary>
public enum SensorState
{
    /// <summary>Switched off, or the head has no way to reach this kind of device.</summary>
    Off,

    /// <summary>
    /// Switched on and looking. For a heart rate monitor this is the normal first half minute of
    /// a session: the watch only advertises while its broadcast toggle is on, because it costs
    /// battery, so the app is usually waiting for a person rather than for a radio.
    /// </summary>
    Searching,

    /// <summary>Connected and reporting.</summary>
    Live,

    /// <summary>Was reporting and has gone quiet. Reconnecting on its own.</summary>
    Lost,

    /// <summary>The permission this needs was refused, so nothing will arrive until it is granted.</summary>
    Denied,
}
