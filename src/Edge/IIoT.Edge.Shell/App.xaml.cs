using IIoT.Edge.Shell.Core;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading;
using System.Windows;
using WpfApplication = System.Windows.Application;

namespace IIoT.Edge.Shell;

public partial class App : WpfApplication
{
    private ServiceProvider? _serviceProvider;
    private readonly CancellationTokenSource _appCts = new();
    private Mutex? _instanceMutex;

    public App()
    {
        _ = typeof(MaterialDesignThemes.Wpf.BundledTheme).Assembly;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        if (!TryAcquireInstanceLock(configuration))
        {
            Shutdown();
            return;
        }

        _serviceProvider = ConfigureServices(configuration).BuildServiceProvider();
        var lifecycle = _serviceProvider.GetRequiredService<IAppLifecycleCoordinator>();
        var startupResult = await lifecycle.StartAsync(_appCts.Token);
        if (!startupResult.Success)
        {
            ShowStartupError(startupResult.Message ?? "应用启动失败。");
            Shutdown(-1);
            return;
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _appCts.Cancel();

            if (_serviceProvider is not null)
            {
                var lifecycle = _serviceProvider.GetRequiredService<IAppLifecycleCoordinator>();
                using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                lifecycle.StopAsync(shutdownCts.Token).GetAwaiter().GetResult();
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }
        }
        catch
        {
        }
        finally
        {
            ReleaseMutex();
            _appCts.Dispose();
            base.OnExit(e);
        }
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

    private static void ShowStartupError(string message)
    {
        MessageBox.Show(
            message,
            "IIoT Edge Client - Startup Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
