using IIoT.Edge.Common.Context;
using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Plc.Factory;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.PlcDevice.Runtime;

public class PlcConnectionManager : IPlcConnectionManager
{
    private readonly IRepository<NetworkDeviceEntity> _networkDevices;
    private readonly IRepository<IoMappingEntity> _ioMappings;
    private readonly IPlcDataStore _dataStore;
    private readonly IPlcServiceFactory _plcServiceFactory;
    private readonly IProductionContextStore _contextStore;
    private readonly ILogService _logger;

    private readonly Dictionary<int, IPlcService> _plcInstances = new();
    private readonly Dictionary<int, List<IPlcTask>> _plcTasks = new();
    private readonly Dictionary<int, CancellationTokenSource> _deviceCtsMap = new();
    private readonly Dictionary<int, List<Task>> _runningTaskHandles = new();

    private readonly Dictionary<string, Func<IPlcBuffer, ProductionContext, List<IPlcTask>>> _taskFactories = new();

    public PlcConnectionManager(
        IRepository<NetworkDeviceEntity> networkDevices,
        IRepository<IoMappingEntity> ioMappings,
        IPlcDataStore dataStore,
        IPlcServiceFactory plcServiceFactory,
        IProductionContextStore contextStore,
        ILogService logger)
    {
        _networkDevices = networkDevices;
        _ioMappings = ioMappings;
        _dataStore = dataStore;
        _plcServiceFactory = plcServiceFactory;
        _contextStore = contextStore;
        _logger = logger;
    }

    public void RegisterTasks(
        string deviceName,
        Func<IPlcBuffer, ProductionContext, List<IPlcTask>> factory)
    {
        _taskFactories[deviceName] = factory;
    }

    public IPlcService? GetPlc(int networkDeviceId)
        => _plcInstances.TryGetValue(networkDeviceId, out var plc) ? plc : null;

    public ProductionContext? GetContext(string deviceName)
        => _contextStore.GetOrCreate(deviceName);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var devices = await _networkDevices.GetListAsync(
            x => x.IsEnabled && x.DeviceType == DeviceType.PLC, ct);

        foreach (var device in devices)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeDeviceAsync(device, ct);
                }
                catch (Exception ex)
                {
                    _logger.Error($"[{device.DeviceName}] 初始化失败: {ex.Message}");
                }
            }, ct);
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

        _dataStore.Register(device.Id, readCount, writeCount);
        var buffer = _dataStore.GetBuffer(device.Id);

        var context = _contextStore.GetOrCreate(device.DeviceName);
        context.DeviceId = device.Id;

        var plcType = Enum.Parse<PlcType>(device.DeviceModel!, ignoreCase: true);
        var plcService = _plcServiceFactory.Create(plcType, device.DeviceName);
        _plcInstances[device.Id] = plcService;

        var deviceCts = new CancellationTokenSource();
        _deviceCtsMap[device.Id] = deviceCts;

        var signalInteraction = new SignalInteraction(
            plcService, _dataStore, device, mappingArray, _logger);

        await signalInteraction.ConnectAsync();

        var tasks = new List<IPlcTask> { signalInteraction };

        if (buffer is not null
            && _taskFactories.TryGetValue(device.DeviceName, out var factory))
        {
            tasks.AddRange(factory(buffer, context));
        }

        _plcTasks[device.Id] = tasks;

        var handles = new List<Task>();
        foreach (var task in tasks)
        {
            var handle = Task.Run(async () =>
            {
                try
                {
                    await task.StartAsync(deviceCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.Error($"[{device.DeviceName}] Task异常: {ex.Message}");
                }
            }, deviceCts.Token);
            handles.Add(handle);
        }
        _runningTaskHandles[device.Id] = handles;

        _logger.Info($"[{device.DeviceName}] 初始化完成，启动 {tasks.Count} 个Task" +
            $"（上下文步骤: {string.Join(", ", context.StepStates.Select(kv => $"{kv.Key}={kv.Value}"))})");
    }

    public async Task ReloadAsync(string deviceName, CancellationToken ct = default)
    {
        var device = (await _networkDevices.GetListAsync(
            x => x.DeviceName == deviceName, ct)).FirstOrDefault();

        if (device is null) return;

        var deviceId = device.Id;

        if (_deviceCtsMap.TryGetValue(deviceId, out var oldCts))
        {
            oldCts.Cancel();

            if (_runningTaskHandles.TryGetValue(deviceId, out var oldHandles) && oldHandles.Count > 0)
            {
                try { await Task.WhenAll(oldHandles).WaitAsync(TimeSpan.FromSeconds(5)); }
                catch { /* timeout，继续强制清理 */ }
                _runningTaskHandles.Remove(deviceId);
            }

            oldCts.Dispose();
            _deviceCtsMap.Remove(deviceId);
        }

        if (_plcTasks.ContainsKey(deviceId))
            _plcTasks.Remove(deviceId);

        if (_plcInstances.TryGetValue(deviceId, out var oldPlc))
        {
            oldPlc.Disconnect();
            oldPlc.Dispose();
            _plcInstances.Remove(deviceId);
        }

        if (!device.IsEnabled) return;

        await InitializeDeviceAsync(device, ct);
        _logger.Info($"[{device.DeviceName}] 热重载完成（上下文已保留）");
    }

    public void Dispose()
    {
        _contextStore.SaveToFile();

        // 1. 取消所有 CTS
        foreach (var cts in _deviceCtsMap.Values)
            cts.Cancel();

        // 2. 等待所有 Task 退出（最多 5 秒），再销毁 PLC
        var allHandles = _runningTaskHandles.Values
            .SelectMany(x => x)
            .ToArray();

        if (allHandles.Length > 0)
        {
            try { Task.WhenAll(allHandles).Wait(TimeSpan.FromSeconds(5)); }
            catch { /* timeout，继续强制清理 */ }
        }

        // 3. 释放 CTS
        foreach (var cts in _deviceCtsMap.Values)
            cts.Dispose();

        _deviceCtsMap.Clear();
        _runningTaskHandles.Clear();

        // 4. 断连并销毁 PLC（Task 已退出，安全操作）
        foreach (var plc in _plcInstances.Values)
        {
            plc.Disconnect();
            plc.Dispose();
        }
        _plcInstances.Clear();
        _plcTasks.Clear();
    }
}
