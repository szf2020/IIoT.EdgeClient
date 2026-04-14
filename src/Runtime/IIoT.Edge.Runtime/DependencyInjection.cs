using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Runtime.Context;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Runtime;

public static class DependencyInjection
{
    public static IServiceCollection AddEdgeRuntime(this IServiceCollection services)
    {
        // 生产上下文管理（按 DeviceName 隔离，JSON 持久化）
        services.AddSingleton<ProductionContextStore>();
        services.AddSingleton<IProductionContextStore>(sp => sp.GetRequiredService<ProductionContextStore>());

        // 当天产能内存存储
        services.AddSingleton<ITodayCapacityStore, TodayCapacityStore>();

        return services;
    }
}