using IIoT.Edge.Infrastructure;
using IIoT.Edge.Infrastructure.Dapper;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading;
using System.Windows;

namespace IIoT.Edge.Shell;

public partial class App : Application
{
    private IServiceProvider _sp = null!;
    private CancellationTokenSource _appCts = new();
    private Mutex? _instanceMutex;

    public App()
    {
        _ = typeof(MaterialDesignThemes.Wpf.BundledTheme).Assembly;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // ── 唯一性校验 ─────────────────────────────────
        if (!TryAcquireInstanceLock(configuration))
        {
            Shutdown();
            return;
        }

        // ── DI 构建 ────────────────────────────────────
        _sp = ConfigureServices(configuration).BuildServiceProvider();

        // ── 基础设施初始化 ─────────────────────────────
        _sp.ApplyMigrations();

        try
        {
            await _sp.InitializeDapperTablesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"数据库初始化失败，程序无法启动。\n\n{ex.Message}",
                "IIoT 客户端 - 启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // ── 生命周期管理 ──────────────────────────────
        var lifecycle = _sp.GetRequiredService<AppLifecycleManager>();
        lifecycle.Initialize();

        var mainWindow = _sp.GetRequiredService<MainWindow>();
        mainWindow.Show();

        lifecycle.StartAll(_sp, _appCts.Token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appCts.Cancel();

        if (_sp is not null)
        {
            var lifecycle = _sp.GetRequiredService<AppLifecycleManager>();
            lifecycle.Shutdown();
        }

        ReleaseMutex();
        base.OnExit(e);

        // 强制杀进程，确保后台线程全部退出
        Environment.Exit(0);
    }

    // ── 私有方法 ──────────────────────────────────────

    private bool TryAcquireInstanceLock(IConfiguration configuration)
    {
        var instanceId = configuration["InstanceId"] ?? "IIoT-Edge-Default";
        var mutexName = $"Global\\IIoT.EdgeClient_{instanceId}";

        _instanceMutex = new Mutex(true, mutexName, out bool createdNew);
        if (createdNew) return true;

        MessageBox.Show(
            $"实例 [{instanceId}] 已在运行中，不能重复启动。",
            "IIoT 客户端",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        _instanceMutex = null;
        return false;
    }

    private void ReleaseMutex()
    {
        if (_instanceMutex is null) return;
        _instanceMutex.ReleaseMutex();
        _instanceMutex.Dispose();
        _instanceMutex = null;
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

    private static string GetDbDirectory()
    {
        var dbDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "data", "db");
        Directory.CreateDirectory(dbDir);
        return dbDir;
    }
}