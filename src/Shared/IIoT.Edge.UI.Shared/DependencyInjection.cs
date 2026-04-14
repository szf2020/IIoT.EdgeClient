using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.UI.Shared;

/// <summary>
/// UI.Shared 依赖注入扩展。
/// 预留共享 UI 基础设施的注册入口。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddUiShared(this IServiceCollection services)
    {
        return services;
    }
}
