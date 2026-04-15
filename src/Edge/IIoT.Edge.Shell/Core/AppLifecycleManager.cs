using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.Recipe;
using IIoT.Edge.Application.Abstractions.Tasks;
using IIoT.Edge.Infrastructure.Persistence.Dapper;
using IIoT.Edge.Infrastructure.Persistence.EfCore;

namespace IIoT.Edge.Shell.Core;

public class AppLifecycleManager : IAppLifecycleCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProductionContextStore _contextStore;
    private readonly IRecipeService _recipeService;
    private readonly IBackgroundServiceCoordinator _backgroundServices;
    private readonly ILogService _logger;

    public AppLifecycleManager(
        IServiceProvider serviceProvider,
        IProductionContextStore contextStore,
        IRecipeService recipeService,
        IBackgroundServiceCoordinator backgroundServices,
        ILogService logger)
    {
        _serviceProvider = serviceProvider;
        _contextStore = contextStore;
        _recipeService = recipeService;
        _backgroundServices = backgroundServices;
        _logger = logger;
    }

    public async Task<AppStartupResult> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info("[Lifecycle] Starting application bootstrap.");

            _serviceProvider.ApplyMigrations();
            _logger.Info("[Lifecycle] EF Core migrations completed.");

            await _serviceProvider.InitializeDapperTablesAsync();
            _logger.Info("[Lifecycle] Dapper tables initialized.");

            _contextStore.LoadFromFile();
            _recipeService.LoadFromFile();
            _logger.Info("[Lifecycle] Restored persisted runtime state.");

            await _backgroundServices.StartAsync(cancellationToken);
            _logger.Info("[Lifecycle] Background services started.");

            return AppStartupResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error($"[Lifecycle] Startup failed: {ex.Message}");
            return AppStartupResult.Failure($"应用启动失败：{ex.Message}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _contextStore.SaveToFile();
        _recipeService.SaveToFile();
        _logger.Info("[Lifecycle] Persisted runtime state before shutdown.");

        await _backgroundServices.StopAsync(cancellationToken);
        _logger.Info("[Lifecycle] Background services stopped.");
    }
}
