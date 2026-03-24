using IIoT.Edge.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.SysLog;

public static class DependencyInjection
{
    public static IServiceCollection AddSysLogModule(
        this IServiceCollection services)
    {
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<LogWidget>();

        return services;
    }
}
