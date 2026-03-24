using IIoT.Edge.Module.Hardware.HardwareConfigView;
using IIoT.Edge.Module.Hardware.IOView;
using IIoT.Edge.Module.Hardware.Plc;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Hardware;

public static class DependencyInjection
{
    public static IServiceCollection AddHardwareModule(
        this IServiceCollection services)
    {
        services.AddSingleton<IOViewWidget>();
        services.AddSingleton<HardwareConfigWidget>();
        services.AddSingleton<PlcConnectionManager>();

        return services;
    }
}