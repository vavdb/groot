using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Groot.UI.Health;
using Java.Util;
using Application = Android.App.Application;

namespace Groot.App.Platforms.Android;

/// <summary>
/// Listens to every heart rate monitor in range over Bluetooth Low Energy.
/// <para>
/// Nothing here is vendor specific. Both an Amazfit watch and a Fitbit Air advertise the standard
/// Heart Rate Service, 0x180D, the same one a Peloton or a chest strap uses, so one client covers
/// every device that can broadcast at all. The phone is the central in this conversation and can
/// hold several monitors at once: the one-connection limit in the vendors' documentation is on
/// how many phones may attach to one watch, not the reverse.
/// </para>
/// <para>
/// A watch only advertises while its own broadcast setting is on, because it costs battery. So
/// the scanner is left running for the whole session rather than stopped once something is found:
/// a monitor switched on two minutes into a warmup has to be able to join.
/// </para>
/// </summary>
public sealed class AndroidHeartRateService : IHeartRateService
{
    // Bluetooth SIG assigned numbers. The 128-bit forms are the 16-bit ids in the base UUID.
    private static readonly UUID HeartRateService = UUID.FromString("0000180d-0000-1000-8000-00805f9b34fb")!;
    private static readonly UUID HeartRateMeasurement = UUID.FromString("00002a37-0000-1000-8000-00805f9b34fb")!;
    private static readonly UUID ClientCharacteristicConfiguration = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")!;

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Monitor> _monitors = [];

    private BluetoothLeScanner? _scanner;
    private ScanCallback? _scanCallback;
    private bool _listening;

    /// <inheritdoc />
    public bool IsSupported => Adapter is not null;

    /// <inheritdoc />
    public SensorState State
    {
        get
        {
            if (!_listening) return SensorState.Off;
            if (_denied) return SensorState.Denied;

            lock (_gate)
            {
                if (_monitors.Values.Any(m => m.State == SensorState.Live)) return SensorState.Live;
                if (_monitors.Values.Any(m => m.State == SensorState.Lost)) return SensorState.Lost;
            }

            return SensorState.Searching;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<HeartRateDevice> Devices
    {
        get
        {
            lock (_gate)
            {
                return _monitors.Values
                    .OrderBy(monitor => monitor.FirstSeen)
                    .Select(monitor => new HeartRateDevice(monitor.Id, monitor.Name, monitor.State, monitor.Bpm))
                    .ToArray();
            }
        }
    }

    private bool _denied;

    private static BluetoothAdapter? Adapter =>
        (Application.Context.GetSystemService(Context.BluetoothService) as BluetoothManager)?.Adapter;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listening) return;

        if (Adapter is not { IsEnabled: true } adapter)
        {
            // The radio is off, or the phone has none. Nothing to do but say so.
            _listening = true;
            _denied = false;
            return;
        }

        _denied = !await BluetoothPermissions.EnsureGrantedAsync();
        _listening = true;
        if (_denied) return;

        _scanner = adapter.BluetoothLeScanner;
        if (_scanner is null) return;

        _scanCallback = new Scanner(this);

        // Filtering on the service uuid rather than sniffing every advertisement in range: on a
        // street that is hundreds of devices a second, and only the ones offering a heart rate
        // are any of our business.
        var filter = new ScanFilter.Builder()!
            .SetServiceUuid(new ParcelUuid(HeartRateService))!
            .Build()!;

        var settings = new ScanSettings.Builder()!
            .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency)!
            .Build()!;

        _scanner.StartScan([filter], settings, _scanCallback);
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        _listening = false;

        if (_scanner is not null && _scanCallback is not null)
        {
            try
            {
                _scanner.StopScan(_scanCallback);
            }
            catch (Exception)
            {
                // The adapter can be switched off between starting and stopping; there is then
                // nothing left to stop and nothing useful to do about it.
            }
        }

        _scanCallback = null;
        _scanner = null;

        lock (_gate)
        {
            foreach (var monitor in _monitors.Values) monitor.Close();
            _monitors.Clear();
        }

        return Task.CompletedTask;
    }

    private void Found(BluetoothDevice device)
    {
        var id = device.Address;
        if (id is null) return;

        lock (_gate)
        {
            if (_monitors.ContainsKey(id)) return;

            var monitor = new Monitor(id, Friendly(device), _monitors.Count);
            _monitors[id] = monitor;
            monitor.Connect(device);
        }
    }

    /// <summary>
    /// What to call the device on screen. A monitor that will not say its name is shown by the
    /// tail of its address, which is at least stable and tells two of them apart.
    /// </summary>
    private static string Friendly(BluetoothDevice device)
    {
        var name = device.Name;
        if (!string.IsNullOrWhiteSpace(name)) return name;

        var address = device.Address ?? "";
        return address.Length >= 5 ? $"Monitor {address[^5..].Replace(":", "")}" : "Monitor";
    }

    /// <summary>One connected monitor: its GATT link, its last reading, and where it stands.</summary>
    private sealed class Monitor(string id, string name, int firstSeen) : BluetoothGattCallback
    {
        private BluetoothGatt? _gatt;

        public string Id { get; } = id;

        public string Name { get; } = name;

        public int FirstSeen { get; } = firstSeen;

        public SensorState State { get; private set; } = SensorState.Searching;

        public int? Bpm { get; private set; }

        public void Connect(BluetoothDevice device) =>
            // autoConnect: the watch drops the link whenever its broadcast setting goes off, and
            // this is what brings it back without the app having to scan for it again.
            _gatt = device.ConnectGatt(Application.Context, autoConnect: true, this);

        public void Close()
        {
            try
            {
                _gatt?.Disconnect();
                _gatt?.Close();
            }
            catch (Exception)
            {
                // Already gone. Nothing to release.
            }

            _gatt = null;
            State = SensorState.Off;
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            switch (newState)
            {
                case ProfileState.Connected:
                    gatt?.DiscoverServices();
                    break;

                case ProfileState.Disconnected:
                    // Not a failure: this is what a watch does when its broadcast toggle goes
                    // off. autoConnect reconnects on its own, so the reading is stale, not gone.
                    State = SensorState.Lost;
                    Bpm = null;
                    break;
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            if (status != GattStatus.Success || gatt is null) return;

            var characteristic = gatt.GetService(HeartRateService)?.GetCharacteristic(HeartRateMeasurement);
            if (characteristic is null) return;

            gatt.SetCharacteristicNotification(characteristic, enable: true);

            // Turning notifications on locally is only half of it: the monitor does not start
            // sending until its own configuration descriptor is written. Without this the
            // connection succeeds and no reading ever arrives.
            var descriptor = characteristic.GetDescriptor(ClientCharacteristicConfiguration);
            if (descriptor is null) return;

            var enable = BluetoothGattDescriptor.EnableNotificationValue!.ToArray();

            // Android 33 moved the value from the descriptor object into the write call itself.
            // Both forms are needed: the app runs from API 24.
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                gatt.WriteDescriptor(descriptor, enable);
            }
            else
            {
                descriptor.SetValue(enable);
                gatt.WriteDescriptor(descriptor);
            }
        }

        /// <summary>The reading, on Android 33 and later, where the bytes come with the callback.</summary>
        public override void OnCharacteristicChanged(
            BluetoothGatt? gatt,
            BluetoothGattCharacteristic characteristic,
            byte[] value) =>
            Receive(characteristic, value);

        /// <summary>The reading, before Android 33, where it has to be read off the characteristic.</summary>
        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            // From 33 the three-argument form is delivered instead, and reading the value off the
            // characteristic is both obsolete and unsafe: the buffer can already have been reused.
            if (OperatingSystem.IsAndroidVersionAtLeast(33) || characteristic is null) return;

            Receive(characteristic, characteristic.GetValue());
        }

        private void Receive(BluetoothGattCharacteristic? characteristic, byte[]? value)
        {
            if (characteristic?.Uuid?.Equals(HeartRateMeasurement) != true) return;
            if (ParseHeartRate(value) is not { } bpm) return;

            Bpm = bpm;
            State = SensorState.Live;
        }

        /// <summary>
        /// The Heart Rate Measurement characteristic, per the Bluetooth SIG profile: a flags byte
        /// followed by the rate. Bit 0 of the flags says whether the rate is one byte or two,
        /// little endian. Everything after it is optional and of no use here.
        /// </summary>
        internal static int? ParseHeartRate(byte[]? value)
        {
            if (value is null || value.Length < 2) return null;

            var sixteenBit = (value[0] & 0x01) != 0;
            if (sixteenBit && value.Length < 3) return null;

            var bpm = sixteenBit ? value[1] | (value[2] << 8) : value[1];
            return bpm is >= 25 and <= 250 ? bpm : null;
        }
    }

    private sealed class Scanner(AndroidHeartRateService owner) : ScanCallback
    {
        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            if (result?.Device is { } device) owner.Found(device);
        }

        public override void OnBatchScanResults(IList<ScanResult>? results)
        {
            foreach (var result in results ?? [])
                if (result.Device is { } device)
                    owner.Found(device);
        }
    }
}
