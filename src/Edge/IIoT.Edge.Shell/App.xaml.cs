using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Infrastructure;
using IIoT.Edge.Infrastructure.Dapper;
using IIoT.Edge.Module.Hardware.Plc;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Tasks.DataPipeline;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace IIoT.Edge.Shell;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    private CancellationTokenSource _appCts = new();

    public App()
    {
        _ = typeof(MaterialDesignThemes.Wpf.BundledTheme).Assembly;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = ConfigureServices(configuration);
        ServiceProvider = services.BuildServiceProvider();
        InitializeInfrastructure();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        StartBackgroundServices();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 保存生产上下文（含产能数据）
        var contextStore = ServiceProvider.GetRequiredService<IProductionContextStore>();
        contextStore.SaveToFile();

        _appCts.Cancel();

        // 停止设备心跳
        var deviceService = ServiceProvider.GetRequiredService<IDeviceService>();
        deviceService.StopAsync().GetAwaiter().GetResult();

        // 停止产能同步
        var capacitySync = ServiceProvider.GetRequiredService<ICapacitySyncTask>();
        capacitySync.StopAsync().GetAwaiter().GetResult();

        var plcManager = ServiceProvider.GetRequiredService<PlcConnectionManager>();
        plcManager.Dispose();

        base.OnExit(e);
    }

    private ServiceCollection ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        var viewRegistry = new ViewRegistry();

        var machineModule = configuration["MachineModule"];
        IModuleLoader loader = new ModuleLoader(services, viewRegistry);
        loader.LoadFromDirectory(
            AppDomain.CurrentDomain.BaseDirectory, machineModule);

        var dbDir = GetDbDirectory();
        services.AddShell(viewRegistry, configuration, dbDir);

        return services;
    }

    private void InitializeInfrastructure()
    {
        ServiceProvider.ApplyMigrations();
        _ = ServiceProvider.InitializeDapperTablesAsync();

        var contextStore = ServiceProvider.GetRequiredService<IProductionContextStore>();
        contextStore.LoadFromFile();
    }

    private void StartBackgroundServices()
    {
        // 上下文自动保存
        var contextStore = ServiceProvider.GetRequiredService<IProductionContextStore>();
        _ = contextStore.StartAutoSaveAsync(_appCts.Token, intervalSeconds: 30);

        // 设备心跳
        var deviceService = ServiceProvider.GetRequiredService<IDeviceService>();
        _ = deviceService.StartAsync(_appCts.Token);

        // PLC 任务
        _ = ServiceProvider.InitializePlcTasksAsync(_appCts.Token);

        // 数据管道
        _ = ServiceProvider.StartDataPipelineAsync(_appCts.Token);

        // 产能定时同步（30分钟）
        var capacitySync = ServiceProvider.GetRequiredService<ICapacitySyncTask>();
        _ = capacitySync.StartAsync(_appCts.Token);
    }

    private static string GetDbDirectory()
    {
        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IIoT.Edge", "db");
        Directory.CreateDirectory(dbDir);
        return dbDir;
    }
}