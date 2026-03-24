// 路径：src/Infrastructure/IIoT.Edge.Tasks/DependencyInjection.cs
using IIoT.Edge.Tasks.Abstractions;
using IIoT.Edge.Tasks.Base;
using IIoT.Edge.Tasks.Context;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Tasks;

public static class DependencyInjection
{
    /// <summary>
    /// 注册 Tasks 类库的核心服务
    /// </summary>
    public static IServiceCollection AddEdgeTasks(this IServiceCollection services)
    {
        // ProductionContextStore 单例：管理所有设备的运行时上下文 + 持久化
        services.AddSingleton<ProductionContextStore>();

        return services;
    }
}