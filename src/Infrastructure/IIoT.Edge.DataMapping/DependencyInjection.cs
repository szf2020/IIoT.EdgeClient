using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.DataMapping;

public static class DependencyInjection
{
    public static IServiceCollection AddDataMapping(this IServiceCollection services)
    {
        // AutoMapper Profile 在 Shell 层统一扫描注册
        // 这里预留后续 MES 映射器等注册位置

        return services;
    }
}