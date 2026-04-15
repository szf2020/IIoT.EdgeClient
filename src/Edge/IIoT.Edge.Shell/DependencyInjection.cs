using IIoT.Edge.Application;
using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Plc;
using IIoT.Edge.Application.Abstractions.Tasks;
using IIoT.Edge.Application.Common.Tasks;
using IIoT.Edge.Infrastructure.DeviceComm;
using IIoT.Edge.Infrastructure.Integration;
using IIoT.Edge.Infrastructure.Persistence.Dapper;
using IIoT.Edge.Infrastructure.Persistence.EfCore;
using IIoT.Edge.Presentation.Navigation;
using IIoT.Edge.Presentation.Panels;
using IIoT.Edge.Presentation.Shell;
using IIoT.Edge.Runtime;
using IIoT.Edge.Runtime.DataPipeline;
using IIoT.Edge.Runtime.DataPipeline.Tasks;
using IIoT.Edge.SharedKernel.DataPipeline.Capacity;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.UI.Shared;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace IIoT.Edge.Shell;

public static class DependencyInjection
{
    public static IServiceCollection AddShell(
        this IServiceCollection services,
        IViewRegistry viewRegistry,
        IConfiguration configuration,
        string dbDir)
    {
        viewRegistry.RegisterNavigationViews();
        viewRegistry.RegisterPanelViews();

        var efDbPath = Path.Combine(dbDir, "edge.db");
        var excelDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "excel");

        Directory.CreateDirectory(dbDir);
        Directory.CreateDirectory(excelDir);

        services.AddSingleton(configuration);
        services.AddSingleton(viewRegistry);
        services.AddSingleton<IViewRegistry>(viewRegistry);

        var shiftConfig = new ShiftConfig();
        configuration.GetSection("Shift").Bind(shiftConfig);
        services.AddSingleton(shiftConfig);

        services.AddEdgeApplication();
        services.AddEfCorePersistenceInfrastructure(efDbPath);
        services.AddDapperPersistenceInfrastructure(dbDir);
        services.AddIntegrationInfrastructure(configuration, excelDir);
        services.AddDeviceCommInfrastructure();
        services.AddEdgeRuntime();
        services.AddEdgeDataPipelineRuntime();

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"] ?? string.Empty;
            cfg.RegisterServicesFromAssemblies(
                typeof(IIoT.Edge.Application.DependencyInjection).Assembly,
                typeof(IIoT.Edge.Presentation.Navigation.DependencyInjection).Assembly,
                typeof(IIoT.Edge.Presentation.Panels.DependencyInjection).Assembly);
        });

        services.AddUiShared();
        services.AddAutoMapper(
            _ => { },
            typeof(IIoT.Edge.Application.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Presentation.Shell.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Presentation.Navigation.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Presentation.Panels.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Infrastructure.Integration.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Infrastructure.DeviceComm.DependencyInjection).Assembly);
        services.AddShellPresentation();
        services.AddNavigationPresentation();
        services.AddPanelPresentation();

        services.AddSingleton<IManagedBackgroundService>(sp =>
            new DelegatingBackgroundService(
                "RuntimeState.AutoSave",
                ct => sp.GetRequiredService<IProductionContextStore>()
                    .StartAutoSaveAsync(ct, intervalSeconds: 30)));

        services.AddSingleton<IManagedBackgroundService>(sp =>
            new DelegatingBackgroundService(
                "Device.Heartbeat",
                ct => sp.GetRequiredService<IDeviceService>().StartAsync(ct),
                _ => sp.GetRequiredService<IDeviceService>().StopAsync()));

        services.AddSingleton<IManagedBackgroundService>(sp =>
            new DelegatingBackgroundService(
                "PLC.Runtime",
                ct => sp.GetRequiredService<IPlcConnectionManager>().InitializeAsync(ct),
                ct => sp.GetRequiredService<IPlcConnectionManager>().StopAsync(ct)));

        services.AddSingleton<IManagedBackgroundService>(sp =>
            new LongRunningBackgroundTaskGroupService(
                "DataPipeline.Runtime",
                new IBackgroundTask[] { sp.GetRequiredService<ProcessQueueTask>() }
                    .Concat(sp.GetServices<RetryTask>())));

        services.AddSingleton<IManagedBackgroundService>(sp =>
            new DelegatingBackgroundService(
                "Cloud.CapacitySync",
                ct => sp.GetRequiredService<ICapacitySyncTask>().StartAsync(ct),
                _ => sp.GetRequiredService<ICapacitySyncTask>().StopAsync()));

        services.AddSingleton<IManagedBackgroundService>(sp =>
            new DelegatingBackgroundService(
                "Cloud.DeviceLogSync",
                ct => sp.GetRequiredService<IDeviceLogSyncTask>().StartAsync(ct),
                _ => sp.GetRequiredService<IDeviceLogSyncTask>().StopAsync()));

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAppLifecycleCoordinator, AppLifecycleManager>();
        services.AddSingleton<AppLifecycleManager>(sp =>
            (AppLifecycleManager)sp.GetRequiredService<IAppLifecycleCoordinator>());
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
