using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.Plc;
using IIoT.Edge.Application.Abstractions.Plc.Factory;
using IIoT.Edge.Application.Abstractions.Plc.Store;
using IIoT.Edge.Domain.Hardware.Aggregates;
using IIoT.Edge.Infrastructure.DeviceComm.Signals;
using IIoT.Edge.SharedKernel.Context;
using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.SharedKernel.Repository;

namespace IIoT.Edge.Infrastructure.DeviceComm.Plc;

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

    public PlcConnectionManager(IRepository<NetworkDeviceEntity> networkDevices, IRepository<IoMappingEntity> ioMappings, IPlcDataStore dataStore, IPlcServiceFactory plcServiceFactory, IProductionContextStore contextStore, ILogService logger)
    {
        _networkDevices = networkDevices;
        _ioMappings = ioMappings;
        _dataStore = dataStore;
        _plcServiceFactory = plcServiceFactory;
        _contextStore = contextStore;
        _logger = logger;
    }

    public void RegisterTasks(string deviceName, Func<IPlcBuffer, ProductionContext, List<IPlcTask>> factory) => _taskFactories[deviceName] = factory;
    public IPlcService? GetPlc(int networkDeviceId) => _plcInstances.TryGetValue(networkDeviceId, out var plc) ? plc : null;
    public ProductionContext? GetContext(string deviceName) => _contextStore.GetOrCreate(deviceName);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var devices = await _networkDevices.GetListAsync(x => x.IsEnabled && x.DeviceType == DeviceType.PLC, ct);
        foreach (var device in devices)
        {
            _ = Task.Run(async () =>
            {
                try { await InitializeDeviceAsync(device, ct); }
                catch (Exception ex) { _logger.Error($"[{device.DeviceName}] Initialization failed: {ex.Message}"); }
            }, ct);
        }
    }

    private async Task InitializeDeviceAsync(NetworkDeviceEntity device, CancellationToken ct)
    {
        var mappings = await _ioMappings.GetListAsync(x => x.NetworkDeviceId == device.Id, ct);
        var mappingArray = mappings.OrderBy(x => x.SortOrder).ToArray();
        var readCount = mappingArray.Where(x => x.Direction == "Read").Sum(x => x.AddressCount);
        var writeCount = mappingArray.Where(x => x.Direction == "Write").Sum(x => x.AddressCount);

        _dataStore.Register(device.Id, readCount, writeCount);
        var buffer = _dataStore.GetBuffer(device.Id);
        var context = _contextStore.GetOrCreate(device.DeviceName);
        context.DeviceId = device.Id;

        var plcType = Enum.Parse<PlcType>(device.DeviceModel!, ignoreCase: true);
        var plcService = _plcServiceFactory.Create(plcType, device.DeviceName);
        _plcInstances[device.Id] = plcService;

        var deviceCts = new CancellationTokenSource();
        _deviceCtsMap[device.Id] = deviceCts;

        var signalInteraction = new SignalInteraction(plcService, _dataStore, device, mappingArray, _logger);
        await signalInteraction.ConnectAsync();

        var tasks = new List<IPlcTask> { signalInteraction };
        if (buffer is not null && _taskFactories.TryGetValue(device.DeviceName, out var factory))
            tasks.AddRange(factory(buffer, context));

        _plcTasks[device.Id] = tasks;

        var handles = new List<Task>();
        foreach (var task in tasks)
        {
            var handle = Task.Run(async () =>
            {
                try { await task.StartAsync(deviceCts.Token); }
                catch (Exception ex) { _logger.Error($"[{device.DeviceName}] Task failed: {ex.Message}"); }
            }, deviceCts.Token);
            handles.Add(handle);
        }

        _runningTaskHandles[device.Id] = handles;
        _logger.Info($"[{device.DeviceName}] Initialized and started {tasks.Count} task(s).");
    }

    public async Task ReloadAsync(string deviceName, CancellationToken ct = default)
    {
        var device = (await _networkDevices.GetListAsync(x => x.DeviceName == deviceName, ct)).FirstOrDefault();
        if (device is null) return;

        var deviceId = device.Id;
        if (_deviceCtsMap.TryGetValue(deviceId, out var oldCts))
        {
            oldCts.Cancel();
            if (_runningTaskHandles.TryGetValue(deviceId, out var oldHandles) && oldHandles.Count > 0)
            {
                try { await Task.WhenAll(oldHandles).WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
                _runningTaskHandles.Remove(deviceId);
            }
            oldCts.Dispose();
            _deviceCtsMap.Remove(deviceId);
        }

        _plcTasks.Remove(deviceId);
        if (_plcInstances.TryGetValue(deviceId, out var oldPlc))
        {
            oldPlc.Disconnect();
            oldPlc.Dispose();
            _plcInstances.Remove(deviceId);
        }

        if (!device.IsEnabled) return;

        await InitializeDeviceAsync(device, ct);
        _logger.Info($"[{device.DeviceName}] Reload completed and context was preserved.");
    }

    public void Dispose()
    {
        _contextStore.SaveToFile();
        foreach (var cts in _deviceCtsMap.Values) cts.Cancel();

        var allHandles = _runningTaskHandles.Values.SelectMany(x => x).ToArray();
        if (allHandles.Length > 0)
        {
            try { Task.WhenAll(allHandles).Wait(TimeSpan.FromSeconds(5)); } catch { }
        }

        foreach (var cts in _deviceCtsMap.Values) cts.Dispose();
        _deviceCtsMap.Clear();
        _runningTaskHandles.Clear();

        foreach (var plc in _plcInstances.Values)
        {
            plc.Disconnect();
            plc.Dispose();
        }

        _plcInstances.Clear();
        _plcTasks.Clear();
    }
}
