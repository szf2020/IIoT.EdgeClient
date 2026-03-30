using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Tasks.Context;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Tasks;

public static class DependencyInjection
{
    public static IServiceCollection AddEdgeTasks(this IServiceCollection services)
    {
        // 生产上下文管理（按 DeviceName 隔离，JSON 持久化）
        services.AddSingleton<ProductionContextStore>();
        services.AddSingleton<IProductionContextStore>(sp => sp.GetRequiredService<ProductionContextStore>());

        // 当天产能内存存储
        services.AddSingleton<ITodayCapacityStore, TodayCapacityStore>();

        return services;
    }
}