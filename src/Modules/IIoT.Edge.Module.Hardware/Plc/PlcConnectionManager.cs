// 路径：src/Modules/IIoT.Edge.Module.Hardware/Plc/PlcConnectionManager.cs
using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Plc.Factory;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Domain.Hardware.Aggregates;
using IIoT.Edge.Tasks.Context;

namespace IIoT.Edge.Module.Hardware.Plc;

public class PlcConnectionManager : IDisposable
{
    private readonly IRepository<NetworkDeviceEntity> _networkDevices;
    private readonly IRepository<IoMappingEntity> _ioMappings;
    private readonly IPlcDataStore _dataStore;
    private readonly IPlcServiceFactory _plcServiceFactory;
    private readonly ProductionContextStore _contextStore;
    private readonly ILogService _logger;

    private readonly Dictionary<int, IPlcService> _plcInstances = new();
    private readonly Dictionary<int, List<IPlcTask>> _plcTasks = new();
    private readonly Dictionary<int, CancellationTokenSource> _deviceCtsMap = new();

    /// <summary>
    /// 任务工厂注册表
    /// 参数：(plcService, buffer, context, mappings)
    /// 返回：该PLC下需要运行的所有任务列表
    /// </summary>
    private readonly Dictionary<int, Func<IPlcService, IPlcBuffer, ProductionContext, IoMappingEntity[], List<IPlcTask>>> _taskFactories = new();

    public PlcConnectionManager(
        IRepository<NetworkDeviceEntity> networkDevices,
        IRepository<IoMappingEntity> ioMappings,
        IPlcDataStore dataStore,
        IPlcServiceFactory plcServiceFactory,
        ProductionContextStore contextStore,
        ILogService logger)
    {
        _networkDevices = networkDevices;
        _ioMappings = ioMappings;
        _dataStore = dataStore;
        _plcServiceFactory = plcServiceFactory;
        _contextStore = contextStore;
        _logger = logger;
    }

    /// <summary>
    /// 注册某台PLC的任务工厂（启动代码里写死调用）
    /// factory 参数说明：
    ///   plcService  — 该PLC的通信服务实例
    ///   buffer      — 该PLC的读写缓冲区
    ///   context     — 该PLC的生产运行时上下文（多任务共享）
    ///   mappings    — 该PLC的IO映射配置
    /// </summary>
    public void RegisterTasks(
        int networkDeviceId,
        Func<IPlcService, IPlcBuffer, ProductionContext, IoMappingEntity[], List<IPlcTask>> factory)
    {
        _taskFactories[networkDeviceId] = factory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var devices = await _networkDevices.GetListAsync(
            x => x.IsEnabled && x.DeviceType == DeviceType.PLC, ct);

        foreach (var device in devices)
        {
            try
            {
                await InitializeDeviceAsync(device, ct);
            }
            catch (Exception ex)
            {
                _logger.Error($"[{device.DeviceName}] 初始化失败: {ex.Message}");
            }
        }
    }

    private async Task InitializeDeviceAsync(NetworkDeviceEntity device, CancellationToken ct)
    {
        var mappings = await _ioMappings.GetListAsync(
            x => x.NetworkDeviceId == device.Id, ct);

        var mappingArray = mappings
            .OrderBy(x => x.SortOrder)
            .ToArray();

        var readCount = mappingArray
            .Where(x => x.Direction == "Read")
            .Sum(x => x.AddressCount);

        var writeCount = mappingArray
            .Where(x => x.Direction == "Write")
            .Sum(x => x.AddressCount);

        // 注册Buffer
        _dataStore.Register(device.Id, readCount, writeCount);
        var buffer = _dataStore.GetBuffer(device.Id);

        // 获取或恢复该设备的生产上下文（重启后自动恢复步骤状态）
        var context = _contextStore.GetOrCreate(device.Id, device.DeviceName);

        // 创建PLC通信实例
        var plcType = Enum.Parse<PlcType>(device.DeviceModel!, ignoreCase: true);
        var plcService = _plcServiceFactory.Create(plcType, device.DeviceName);
        _plcInstances[device.Id] = plcService;

        // 创建该设备独立的CTS
        var deviceCts = new CancellationTokenSource();
        _deviceCtsMap[device.Id] = deviceCts;

        // SignalInteraction 始终是第一个任务（数据搬运）
        var signalInteraction = new SignalInteraction(
            plcService, _dataStore, device, mappingArray, _logger);

        await signalInteraction.ConnectAsync();

        var tasks = new List<IPlcTask> { signalInteraction };

        // 通过注册的工厂创建业务任务，传入 buffer + context
        if (buffer is not null
            && _taskFactories.TryGetValue(device.Id, out var factory))
        {
            tasks.AddRange(factory(plcService, buffer, context, mappingArray));
        }

        _plcTasks[device.Id] = tasks;

        // 每个任务用该设备自己的CTS启动
        foreach (var task in tasks)
            await task.StartAsync(deviceCts.Token);

        _logger.Info($"[{device.DeviceName}] 初始化完成，启动 {tasks.Count} 个Task" +
            $"（上下文步骤: {string.Join(", ", context.StepStates.Select(kv => $"{kv.Key}={kv.Value}"))})");
    }

    public async Task ReloadAsync(int networkDeviceId, CancellationToken ct = default)
    {
        // 只取消该设备的任务
        if (_deviceCtsMap.TryGetValue(networkDeviceId, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
            _deviceCtsMap.Remove(networkDeviceId);
        }

        if (_plcTasks.ContainsKey(networkDeviceId))
            _plcTasks.Remove(networkDeviceId);

        if (_plcInstances.TryGetValue(networkDeviceId, out var oldPlc))
        {
            oldPlc.Disconnect();
            oldPlc.Dispose();
            _plcInstances.Remove(networkDeviceId);
        }

        // 注意：不清除 ProductionContext，热重载后任务恢复到之前的步骤继续执行
        var device = await _networkDevices.GetByIdAsync(networkDeviceId, ct);
        if (device is null || !device.IsEnabled) return;

        await InitializeDeviceAsync(device, ct);
        _logger.Info($"[{device.DeviceName}] 热重载完成（上下文已保留）");
    }

    public IPlcService? GetPlc(int networkDeviceId)
        => _plcInstances.TryGetValue(networkDeviceId, out var plc) ? plc : null;

    /// <summary>
    /// 获取指定设备的生产上下文（供外部查询，如UI展示）
    /// </summary>
    public ProductionContext? GetContext(int networkDeviceId)
        => _contextStore.GetOrCreate(networkDeviceId);

    public void Dispose()
    {
        // 停止前先持久化所有上下文
        _contextStore.SaveToFile();

        foreach (var cts in _deviceCtsMap.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _deviceCtsMap.Clear();

        foreach (var plc in _plcInstances.Values)
        {
            plc.Disconnect();
            plc.Dispose();
        }
        _plcInstances.Clear();
        _plcTasks.Clear();
    }
}