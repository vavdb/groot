namespace Groot.App.Platforms.Android;

/// <summary>
/// The runtime permissions a Bluetooth scan needs. Android 12 split the old blanket Bluetooth
/// permission into SCAN and CONNECT, and before that a scan counted as a location lookup, so the
/// two eras ask for different things. MAUI has no built-in permission for either.
/// </summary>
public sealed class BluetoothPermissions : Permissions.BasePlatformPermission
{
    /// <inheritdoc />
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(31)
            ?
            [
                (global::Android.Manifest.Permission.BluetoothScan, true),
                (global::Android.Manifest.Permission.BluetoothConnect, true),
            ]
            :
            [
                // Below Android 12 a scan can reveal where the phone is, so it needed the
                // location permission. The manifest declares the old Bluetooth permissions with
                // a maxSdkVersion, and those are install-time rather than runtime.
                (global::Android.Manifest.Permission.AccessFineLocation, true),
            ];

    /// <summary>
    /// Asks for what a scan needs, and answers whether it was granted. Never throws: a refused
    /// permission is an ordinary state for the run screen to show, not an error to handle.
    /// </summary>
    public static async Task<bool> EnsureGrantedAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<BluetoothPermissions>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<BluetoothPermissions>();

            return status == PermissionStatus.Granted;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
