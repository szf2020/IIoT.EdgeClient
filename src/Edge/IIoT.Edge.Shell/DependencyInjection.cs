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
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Infrastructure.Excel;
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
        var excelDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IIoT.Edge", "excel");
        services.AddInfrastructure(efDbPath);
        services.AddDapperInfrastructure(dbDir);
        services.AddExcelInfrastructure(excelDir);
        services.AddCloudSync(configuration);
        services.AddPlcDevice();

        // ── Tasks 层 ────────────────────────────────
        services.AddEdgeTasks();
        services.AddDataPipeline();

        // ── MediatR ─────────────────────────────────
        // ── MediatR ─────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODA0MTE4NDAwIiwiaWF0IjoiMTc3MjYwOTI4MCIsImFjY291bnRfaWQiOiIwMTljYjdiYTA0NGM3Y2FjYTcyZDNhMWQ3YjRlYzZjNiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa2p2dnk2ZjFjdjM1bmF3NzNmNGZ0MTE4Iiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.vuSHJIt34rSumtJdD5ZI6gorKQmaD5Msk28ucJr2GIPFR1TsOqtyvdMydzyN5nFIEv_EeGNOu_LfTHTCDz2G-Vu9atS1h7xhIoQqNT8PvuLPHEHrf90YjOKEe4rxjohth1fC2SqpkvrJ0VzEPWQNsy5lvoLOZmzw2WAHa6NBy5bc4R9tQNwOUUbxLSwhmnyOo6K1Td87CBXEjAveGrXuSwhNE0NnQWuTs1ptcK40tfkq3T3Bigh2NO-QDiGuipxoS5AQIkO6n-wLjuhFW1078IEeyh9wct2l7s8htWNQLIlmRvFFJPiN2m1-cI60ds4SYfr4FA4pM6DSNXIMDMeGyA";
            cfg.RegisterServicesFromAssemblies(
     typeof(IIoT.Edge.Module.Config.UseCases.SystemConfig.Queries.GetAllSystemConfigsHandler).Assembly,
     typeof(IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Queries.GetAllNetworkDevicesHandler).Assembly,
     typeof(IIoT.Edge.Module.Production.EventHandlers.CellCompletedEventHandler).Assembly
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