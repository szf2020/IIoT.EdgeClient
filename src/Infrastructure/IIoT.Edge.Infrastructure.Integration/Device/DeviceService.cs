using System.Net.Http.Json;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Infrastructure.Integration.Config;
using IIoT.Edge.Infrastructure.Integration.Device.Cache;

namespace IIoT.Edge.Infrastructure.Integration.Device;

public class DeviceService : IDeviceService
{
    private readonly HttpClient _httpClient;
    private readonly ICloudApiEndpointProvider _endpointProvider;
    private readonly IDeviceInstanceIdResolver _instanceIdResolver;
    private readonly DeviceSessionFileCacheStore _cacheStore;
    private readonly ILogService _logger;
    private readonly object _stateLock = new();
    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;
    private static readonly TimeSpan OnlineInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan OfflineInterval = TimeSpan.FromSeconds(10);

    public DeviceSession? CurrentDevice { get; private set; }
    public NetworkState CurrentState { get; private set; } = NetworkState.Offline;
    public bool HasDeviceId => CurrentDevice is not null;
    public event Action<NetworkState>? NetworkStateChanged;
    public event Action<DeviceSession?>? DeviceIdentified;

    public DeviceService(HttpClient httpClient, ICloudApiEndpointProvider endpointProvider, IDeviceInstanceIdResolver instanceIdResolver, DeviceSessionFileCacheStore cacheStore, ILogService logger)
    {
        _httpClient = httpClient;
        _endpointProvider = endpointProvider;
        _instanceIdResolver = instanceIdResolver;
        _cacheStore = cacheStore;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_heartbeatTask is not null)
            {
                try { await _heartbeatTask; } catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        _logger.Info("[DeviceService] Heartbeat loop started.");
        await IdentifyOnceAsync();

        while (!ct.IsCancellationRequested)
        {
            var interval = CurrentState == NetworkState.Online ? OnlineInterval : OfflineInterval;
            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { break; }
            await IdentifyOnceAsync();
        }

        _logger.Info("[DeviceService] Heartbeat loop stopped.");
    }

    private async Task IdentifyOnceAsync()
    {
        var instanceId = _instanceIdResolver.ResolveInstanceId();
        try
        {
            var clientCode = _endpointProvider.GetClientCode();
            var deviceInstancePath = _endpointProvider.GetDeviceInstancePath();
            var url = _endpointProvider.BuildUrl($"{deviceInstancePath}?macAddress={Uri.EscapeDataString(instanceId)}&clientCode={Uri.EscapeDataString(clientCode)}");

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn($"[DeviceService] Device identify failed: {response.StatusCode}");
                GoOffline(instanceId);
                return;
            }

            var dto = await response.Content.ReadFromJsonAsync<DeviceResponseDto>().ConfigureAwait(false);
            if (dto is null)
            {
                _logger.Warn("[DeviceService] Device identify returned empty payload.");
                GoOffline(instanceId);
                return;
            }

            var session = new DeviceSession
            {
                DeviceId = dto.Id,
                DeviceName = dto.DeviceName,
                MacAddress = instanceId,
                ProcessId = dto.ProcessId
            };

            GoOnline(session);
        }
        catch (TaskCanceledException)
        {
            _logger.Warn("[DeviceService] Device identify timeout.");
            GoOffline(instanceId);
        }
        catch (HttpRequestException ex)
        {
            _logger.Warn($"[DeviceService] Network exception: {ex.Message}");
            GoOffline(instanceId);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DeviceService] Identify exception: {ex.Message}");
            GoOffline(instanceId);
        }
    }

    private void GoOnline(DeviceSession session)
    {
        lock (_stateLock)
        {
            var deviceChanged = CurrentDevice is null || CurrentDevice.DeviceId != session.DeviceId || CurrentDevice.DeviceName != session.DeviceName || CurrentDevice.ProcessId != session.ProcessId;
            CurrentDevice = session;

            try { _cacheStore.Save(session); }
            catch (Exception ex) { _logger.Warn($"[DeviceService] Save cache failed: {ex.Message}"); }

            if (deviceChanged)
            {
                _logger.Info($"[DeviceService] Device updated: {session.DeviceName}");
                DeviceIdentified?.Invoke(CurrentDevice);
            }

            if (CurrentState != NetworkState.Online)
            {
                CurrentState = NetworkState.Online;
                _logger.Info("[DeviceService] State changed to Online.");
                NetworkStateChanged?.Invoke(NetworkState.Online);
            }
        }
    }

    private void GoOffline(string instanceId)
    {
        lock (_stateLock)
        {
            if (CurrentDevice is null)
            {
                try
                {
                    var cached = _cacheStore.TryLoad(instanceId);
                    if (cached is not null)
                    {
                        CurrentDevice = cached;
                        _logger.Info($"[DeviceService] Loaded local cache: {cached.DeviceName}");
                        DeviceIdentified?.Invoke(CurrentDevice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[DeviceService] Load cache failed: {ex.Message}");
                }
            }

            if (CurrentState != NetworkState.Offline)
            {
                CurrentState = NetworkState.Offline;
                _logger.Info("[DeviceService] State changed to Offline.");
                NetworkStateChanged?.Invoke(NetworkState.Offline);
            }
        }
    }
}
