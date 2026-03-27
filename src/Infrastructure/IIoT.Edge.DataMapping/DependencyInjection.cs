using IIoT.Edge.DataMapping.Cloud;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.DataMapping;

public static class DependencyInjection
{
    public static IServiceCollection AddDataMapping(this IServiceCollection services)
    {
        // ── 云端映射器 ─────────────────────────────────────────
        services.AddSingleton<InjectionCloudMapper>();

        // ── MES 映射器（后面做） ───────────────────────────────
        // services.AddSingleton<InjectionMesMapper>();

        return services;
    }
}