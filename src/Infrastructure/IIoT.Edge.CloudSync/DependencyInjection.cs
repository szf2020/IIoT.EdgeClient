using IIoT.Edge.CloudSync.Auth;
using IIoT.Edge.CloudSync.Capacity;
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.CloudSync.Device;
using IIoT.Edge.CloudSync.PassStation;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.Device;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.CloudSync;

public static class DependencyInjection
{
    public static IServiceCollection AddCloudSync(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["CloudApi:BaseUrl"] ?? "http://10.98.90.154:81";
        var timeout = TimeSpan.FromSeconds(3);

        // ── 本地紧急登录配置 ────────────────────────────────────
        var localAdminConfig = new LocalAdminConfig();
        configuration.GetSection("LocalAdmin").Bind(localAdminConfig);
        services.AddSingleton(localAdminConfig);

        // ── 人员认证 ───────────────────────────────────────────
        services.AddHttpClient<AuthService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = timeout;
        });
        services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<AuthService>());

        // ── 设备心跳寻址 ───────────────────────────────────────
        services.AddHttpClient<DeviceService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = timeout;
        });
        services.AddSingleton<IDeviceService>(sp => sp.GetRequiredService<DeviceService>());

        // ── 云端数据上报 HttpClient ────────────────────────────
        services.AddHttpClient("CloudApi", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = timeout;
        });

        // ── 云端上报消费者 ─────────────────────────────────────
        services.AddSingleton<ICloudConsumer, CloudConsumer>();

        // ── 产能消费者 ─────────────────────────────────────────
        services.AddSingleton<ICapacityConsumer, CapacityConsumer>();

        // ── 产能定时同步 ───────────────────────────────────────
        services.AddSingleton<ICapacitySyncTask, CapacitySyncTask>();

        return services;
    }
}