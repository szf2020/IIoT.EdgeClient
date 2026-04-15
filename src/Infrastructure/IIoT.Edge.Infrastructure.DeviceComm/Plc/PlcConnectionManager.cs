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
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private bool _disposed;

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

    public void RegisterTasks(string deviceName, Func<IPlcBuffer, ProductionContext, List<IPlcTask>> factory)
    {
        lock (_stateLock)
        {
            _taskFactories[deviceName] = factory;
        }
    }

    public IPlcService? GetPlc(int networkDeviceId)
    {
        lock (_stateLock)
        {
            _plcInstances.TryGetValue(networkDeviceId, out var plc);
            return plc;
        }
    }

    public ProductionContext? GetContext(string deviceName) => _contextStore.GetOrCreate(deviceName);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            ThrowIfDisposed();
            var devices = await _networkDevices.GetListAsync(
                x => x.IsEnabled && x.DeviceType == DeviceType.PLC,
                ct);

            foreach (var device in devices)
            {
                await InitializeDeviceSafelyAsync(device, ct);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task InitializeDeviceSafelyAsync(NetworkDeviceEntity device, CancellationToken ct)
    {
        try
        {
            await InitializeDeviceAsync(device, ct);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{device.DeviceName}] Initialization failed: {ex.Message}");
        }
    }

    private async Task InitializeDeviceAsync(NetworkDeviceEntity device, CancellationToken ct)
    {
        ThrowIfDisposed();

        lock (_stateLock)
        {
            if (_deviceCtsMap.ContainsKey(device.Id))
            {
                _logger.Info($"[{device.DeviceName}] Skipped initialization because the device is already running.");
                return;
            }
        }

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
        var deviceCts = new CancellationTokenSource();

        var signalInteraction = new SignalInteraction(plcService, _dataStore, device, mappingArray, _logger);
        await signalInteraction.ConnectAsync();

        var tasks = new List<IPlcTask> { signalInteraction };
        Func<IPlcBuffer, ProductionContext, List<IPlcTask>>? factory = null;
        lock (_stateLock)
        {
            _taskFactories.TryGetValue(device.DeviceName, out factory);
        }

        if (buffer is not null && factory is not null)
        {
            tasks.AddRange(factory(buffer, context));
        }

        var handles = new List<Task>();
        foreach (var task in tasks)
        {
            var handle = Task.Run(async () =>
            {
                try
                {
                    await task.StartAsync(deviceCts.Token);
                }
                catch (OperationCanceledException) when (deviceCts.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.Error($"[{device.DeviceName}] Task failed: {ex.Message}");
                }
            }, CancellationToken.None);

            handles.Add(handle);
        }

        lock (_stateLock)
        {
            if (_disposed)
            {
                deviceCts.Cancel();
                return;
            }

            _plcInstances[device.Id] = plcService;
            _deviceCtsMap[device.Id] = deviceCts;
            _plcTasks[device.Id] = tasks;
            _runningTaskHandles[device.Id] = handles;
        }

        _logger.Info($"[{device.DeviceName}] Initialized and started {tasks.Count} task(s).");
    }

    public async Task ReloadAsync(string deviceName, CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            ThrowIfDisposed();
            var device = (await _networkDevices.GetListAsync(x => x.DeviceName == deviceName, ct)).FirstOrDefault();
            if (device is null)
            {
                _logger.Warn($"[{deviceName}] Reload skipped because the device was not found.");
                return;
            }

            await StopDeviceCoreAsync(device.Id, ct);
            if (!device.IsEnabled)
            {
                _logger.Info($"[{device.DeviceName}] Reload finished: device is disabled.");
                return;
            }

            await InitializeDeviceAsync(device, ct);
            _logger.Info($"[{device.DeviceName}] Reload completed and context was preserved.");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            _contextStore.SaveToFile();
            var deviceIds = GetTrackedDeviceIdsSnapshot();
            foreach (var deviceId in deviceIds)
            {
                await StopDeviceCoreAsync(deviceId, ct);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task StopDeviceCoreAsync(int deviceId, CancellationToken ct)
    {
        CancellationTokenSource? deviceCts = null;
        List<Task>? runningHandles = null;
        IPlcService? plc = null;

        lock (_stateLock)
        {
            if (_deviceCtsMap.TryGetValue(deviceId, out var cts))
            {
                deviceCts = cts;
                _deviceCtsMap.Remove(deviceId);
            }

            if (_runningTaskHandles.TryGetValue(deviceId, out var handles))
            {
                runningHandles = handles;
                _runningTaskHandles.Remove(deviceId);
            }

            _plcTasks.Remove(deviceId);

            if (_plcInstances.TryGetValue(deviceId, out var instance))
            {
                plc = instance;
                _plcInstances.Remove(deviceId);
            }
        }

        if (deviceCts is not null)
        {
            deviceCts.Cancel();
        }

        if (runningHandles is not null && runningHandles.Count > 0)
        {
            try
            {
                await Task.WhenAll(runningHandles).WaitAsync(TimeSpan.FromSeconds(5), ct);
            }
            catch
            {
            }
        }

        if (deviceCts is not null)
        {
            deviceCts.Dispose();
        }

        if (plc is not null)
        {
            plc.Disconnect();
            plc.Dispose();
        }
    }

    private int[] GetTrackedDeviceIdsSnapshot()
    {
        lock (_stateLock)
        {
            return _deviceCtsMap.Keys
                .Concat(_plcInstances.Keys)
                .Concat(_runningTaskHandles.Keys)
                .Distinct()
                .ToArray();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            StopAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch
        {
        }
        finally
        {
            _disposed = true;
            _lifecycleGate.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PlcConnectionManager));
        }
    }
}
