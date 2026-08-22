using Groot.App.Audio;
using Groot.UI.Audio;
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

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
