// 路径：src/Edge/IIoT.Edge.Shell/DependencyInjection.cs
using IIoT.Edge.CloudSync;
using IIoT.Edge.Infrastructure;
using IIoT.Edge.Infrastructure.Dapper;
using IIoT.Edge.Module.Config;
using IIoT.Edge.Module.Config.ParamView.Mappings;
using IIoT.Edge.Module.Formula;
using IIoT.Edge.Module.Hardware;
using IIoT.Edge.Module.Hardware.HardwareConfigView.Mappings;
using IIoT.Edge.Module.Hardware.Plc;
using IIoT.Edge.Module.Production;
using IIoT.Edge.Module.SysLog;
using IIoT.Edge.PlcDevice;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.Tasks;
using IIoT.Edge.Tasks.DataPipeline;
using IIoT.Edge.UI.Shared;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Module.Config.UseCases.SystemConfig.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace IIoT.Edge.Shell;

public static class DependencyInjection
{
    /// <summary>
    /// 注册所有层的服务
    /// </summary>
    public static IServiceCollection AddShell(
        this IServiceCollection services,
        IViewRegistry viewRegistry,
        IConfiguration configuration,
        string dbDir)
    {
        // ── 数据库路径 ───────────────────────────────
        var efDbPath = Path.Combine(dbDir, "edge.db");

        // ── 基础设施层 ──────────────────────────────
        services.AddInfrastructure(efDbPath);
        services.AddDapperInfrastructure(dbDir);
        services.AddCloudSync(configuration);
        services.AddPlcDevice();

        // ── Tasks 层 ────────────────────────────────
        services.AddEdgeTasks();
        services.AddDataPipeline();

        // ── MediatR ─────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = "eyJhbGci...（省略）";
            cfg.RegisterServicesFromAssemblies(
                typeof(IIoT.Edge.Module.Config.UseCases.SystemConfig.Queries.GetAllSystemConfigsQuery).Assembly,
                typeof(IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Queries.GetAllNetworkDevicesQuery).Assembly
            );
        });

        // ── UI 层 ───────────────────────────────────
        services.AddShellWidgets();
        services.AddHardwareModule();
        services.AddProductionModule();
        services.AddConfigModule();
        services.AddFormulaModule();
        services.AddSysLogModule();

        // ── Shell 自身 ──────────────────────────────
        services.AddSingleton(viewRegistry);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<HardwareMappingProfile>();
            cfg.AddProfile<ConfigMappingProfile>();
        });
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }

    /// <summary>
    /// 容器构建后调用：注册PLC任务组合并初始化
    /// </summary>
    public static async Task InitializePlcTasksAsync(
        this IServiceProvider sp, CancellationToken ct = default)
    {
        var plcManager = sp.GetRequiredService<PlcConnectionManager>();
        var logService = sp.GetRequiredService<ILogService>();

        await plcManager.InitializeAsync(ct);
        logService.Info("PLC任务体系初始化完成");
    }
}