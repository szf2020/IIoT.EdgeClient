using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.Tasks.DataPipeline;

namespace IIoT.Edge.Shell.Core;

/// <summary>
/// 应用生命周期管理器
/// </summary>
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
        _logger.Info("[Lifecycle] 生产上下文 + 配方数据已加载");
    }

    public void StartAll(IServiceProvider sp, CancellationToken ct)
    {
        _backgroundTasks.Add(_contextStore.StartAutoSaveAsync(ct, intervalSeconds: 30));
        _backgroundTasks.Add(_deviceService.StartAsync(ct));
        _backgroundTasks.Add(sp.InitializePlcTasksAsync(ct));
        _backgroundTasks.Add(sp.StartDataPipelineAsync(ct));
        _backgroundTasks.Add(_capacitySync.StartAsync(ct));
        _backgroundTasks.Add(_deviceLogSync.StartAsync(ct));

        _logger.Info("[Lifecycle] 所有后台服务已启动");
    }

    public void Shutdown()
    {
        _contextStore.SaveToFile();
        _recipeService.SaveToFile();
        _logger.Info("[Lifecycle] 生产上下文 + 配方数据已保存");

        // 等待所有后台 Task 在 CancellationToken 取消后退出（最多 8 秒）
        if (_backgroundTasks.Count > 0)
        {
            try { Task.WhenAll(_backgroundTasks).Wait(TimeSpan.FromSeconds(8)); }
            catch { /* OperationCanceledException / timeout，继续强制清理 */ }
        }

        _plcManager.Dispose();

        _logger.Info("[Lifecycle] 所有服务已停止");
    }
}