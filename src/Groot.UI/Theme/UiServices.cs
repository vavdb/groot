using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Groot.UI.Theme;

/// <summary>
/// Registers MudBlazor for a host (Web, App, Gallery) with Groot's defaults: snackbars at
/// bottom-center. Hosts call <c>services.AddGrootUI()</c> once in their composition root.
/// </summary>
public static class UiServices
{
    public static IServiceCollection AddGrootUI(this IServiceCollection services)
    {
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
        });
        return services;
    }
}
