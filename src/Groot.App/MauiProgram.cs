using Groot.App.Audio;
using Groot.UI.Audio;
using Groot.UI.Health;
using Groot.UI.Theme;
using Microsoft.Extensions.Logging;

namespace Groot.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddGrootUI();

		// On-device voice: TTS everywhere + Android beeps (see Audio/MauiCuePlayer.cs).
		builder.Services.AddSingleton<ICuePlayer, MauiCuePlayer>();

		// The phone's own SQLite file. One instance: it holds the opened database and the
		// account, and applying the schema twice would be work for nothing.
		builder.Services.AddSingleton<Storage.GrootStorage>();

		// Heart rate monitors and the phone's own position. Android only for now: the other
		// platforms fall back to services that report themselves unsupported, which leaves the
		// run screen exactly as it was before any of this existed.
#if SIMULATE_SENSORS
		// -p:SimulateSensors=true. The emulator has no Bluetooth adapter, so this is the only way
		// to exercise the trace, the zones and what reaches the database without a watch. It says
		// nothing about whether the real client works; that still needs one.
		builder.Services.AddSingleton<IHeartRateService, SimulatedHeartRateService>();
#elif ANDROID
		builder.Services.AddSingleton<IHeartRateService, Platforms.Android.AndroidHeartRateService>();
#else
		builder.Services.AddSingleton<IHeartRateService, NoHeartRateService>();
#endif

#if ANDROID
		builder.Services.AddSingleton<ILocationService, Platforms.Android.AndroidLocationService>();
#else
		builder.Services.AddSingleton<IHeartRateService, NoHeartRateService>();
		builder.Services.AddSingleton<ILocationService, NoLocationService>();
#endif

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
