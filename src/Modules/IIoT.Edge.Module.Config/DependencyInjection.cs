using IIoT.Edge.Module.Config.ParamView;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Config;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigModule(
        this IServiceCollection services)
    {
        services.AddSingleton<ParamViewWidget>();

        return services;
    }
}