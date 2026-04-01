using IIoT.Edge.Module.Hardware.HardwareConfigView;
using IIoT.Edge.Module.Hardware.IOView;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Hardware;

public static class DependencyInjection
{
    public static IServiceCollection AddHardwareModule(
        this IServiceCollection services)
    {
        services.AddSingleton<IOViewWidget>();
        services.AddSingleton<HardwareConfigWidget>();

        return services;
    }
}