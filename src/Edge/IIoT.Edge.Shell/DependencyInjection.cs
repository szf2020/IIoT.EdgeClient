using IIoT.Edge.Application;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.Plc;
using IIoT.Edge.Infrastructure.DeviceComm;
using IIoT.Edge.Infrastructure.Integration;
using IIoT.Edge.Infrastructure.Persistence.Dapper;
using IIoT.Edge.Infrastructure.Persistence.EfCore;
using IIoT.Edge.Presentation.Navigation;
using IIoT.Edge.Presentation.Panels;
using IIoT.Edge.Presentation.Shell;
using IIoT.Edge.Runtime;
using IIoT.Edge.Runtime.DataPipeline;
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
            typeof(IIoT.Edge.Application.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Presentation.Shell.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Presentation.Navigation.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Presentation.Panels.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Infrastructure.Integration.DependencyInjection).Assembly,
            typeof(IIoT.Edge.Infrastructure.DeviceComm.DependencyInjection).Assembly);
        services.AddShellPresentation();
        services.AddNavigationPresentation();
        services.AddPanelPresentation();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<AppLifecycleManager>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }

    public static async Task InitializePlcTasksAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        var plcManager = sp.GetRequiredService<IPlcConnectionManager>();
        var logService = sp.GetRequiredService<ILogService>();

        await plcManager.InitializeAsync(ct);
        logService.Info("PLC initialization completed.");
    }
}
