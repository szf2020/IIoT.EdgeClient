using IIoT.Edge.Module.Config.ParamView.Mappings;
using IIoT.Edge.Module.Hardware.HardwareConfigView.Mappings;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Shell;

public static class DependencyInjection
{
    public static IServiceCollection AddShell(
        this IServiceCollection services,
        IViewRegistry viewRegistry)
    {
        services.AddSingleton(viewRegistry);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<HardwareMappingProfile>();
            cfg.AddProfile<ConfigMappingProfile>();
        });

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}