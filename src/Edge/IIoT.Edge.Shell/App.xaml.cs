using IIoT.Edge.Infrastructure.Persistence.Dapper;
using IIoT.Edge.Infrastructure.Persistence.EfCore;
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
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        if (!TryAcquireInstanceLock(configuration))
        {
            Shutdown();
            return;
        }

        _sp = ConfigureServices(configuration).BuildServiceProvider();
        _sp.ApplyMigrations();

        try
        {
            await _sp.InitializeDapperTablesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Database initialization failed.\n\n{ex.Message}",
                "IIoT Edge Client - Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

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
        Environment.Exit(0);
    }

    private bool TryAcquireInstanceLock(IConfiguration configuration)
    {
        var instanceId = configuration["InstanceId"] ?? "IIoT-Edge-Default";
        var mutexName = $"Global\\IIoT.EdgeClient_{instanceId}";

        _instanceMutex = new Mutex(true, mutexName, out var createdNew);
        if (createdNew)
        {
            return true;
        }

        MessageBox.Show(
            $"Instance [{instanceId}] is already running.",
            "IIoT Edge Client",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        _instanceMutex = null;
        return false;
    }

    private void ReleaseMutex()
    {
        if (_instanceMutex is null)
        {
            return;
        }

        _instanceMutex.ReleaseMutex();
        _instanceMutex.Dispose();
        _instanceMutex = null;
    }

    private ServiceCollection ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        var viewRegistry = new ViewRegistry();
        var dbDir = GetDbDirectory();
        services.AddShell(viewRegistry, configuration, dbDir);
        return services;
    }

    private static string GetDbDirectory()
    {
        var dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "db");
        Directory.CreateDirectory(dbDir);
        return dbDir;
    }
}
