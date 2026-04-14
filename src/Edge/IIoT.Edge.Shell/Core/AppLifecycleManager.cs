using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.Plc;
using IIoT.Edge.Application.Abstractions.Recipe;
using IIoT.Edge.Runtime.DataPipeline;

namespace IIoT.Edge.Shell.Core;

public class AppLifecycleManager
{
    private readonly IProductionContextStore _contextStore;
    private readonly IDeviceService _deviceService;
    private readonly ICapacitySyncTask _capacitySync;
    private readonly IDeviceLogSyncTask _deviceLogSync;
    private readonly IRecipeService _recipeService;
    private readonly IPlcConnectionManager _plcManager;
    private readonly ILogService _logger;
    private readonly List<Task> _backgroundTasks = new();

    public AppLifecycleManager(
        IProductionContextStore contextStore,
        IDeviceService deviceService,
        ICapacitySyncTask capacitySync,
        IDeviceLogSyncTask deviceLogSync,
        IRecipeService recipeService,
        IPlcConnectionManager plcManager,
        ILogService logger)
    {
        _contextStore = contextStore;
        _deviceService = deviceService;
        _capacitySync = capacitySync;
        _deviceLogSync = deviceLogSync;
        _recipeService = recipeService;
        _plcManager = plcManager;
        _logger = logger;
    }

    public void Initialize()
    {
        _contextStore.LoadFromFile();
        _recipeService.LoadFromFile();
        _logger.Info("[Lifecycle] Restored persisted runtime state.");
    }

    public void StartAll(IServiceProvider sp, CancellationToken ct)
    {
        _backgroundTasks.Add(_contextStore.StartAutoSaveAsync(ct, intervalSeconds: 30));
        _backgroundTasks.Add(_deviceService.StartAsync(ct));
        _backgroundTasks.Add(sp.InitializePlcTasksAsync(ct));
        _backgroundTasks.Add(sp.StartEdgeDataPipelineRuntimeAsync(ct));
        _backgroundTasks.Add(_capacitySync.StartAsync(ct));
        _backgroundTasks.Add(_deviceLogSync.StartAsync(ct));

        _logger.Info("[Lifecycle] Background services started.");
    }

    public void Shutdown()
    {
        _contextStore.SaveToFile();
        _recipeService.SaveToFile();
        _logger.Info("[Lifecycle] Persisted runtime state before shutdown.");

        if (_backgroundTasks.Count > 0)
        {
            try
            {
                Task.WhenAll(_backgroundTasks).Wait(TimeSpan.FromSeconds(8));
            }
            catch
            {
            }
        }

        _plcManager.Dispose();
        _logger.Info("[Lifecycle] Background services stopped.");
    }
}
