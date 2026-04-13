using System.Net.Http.Json;
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.CloudSync.Device;

/// <summary>
/// 设备服务实现
/// 
/// 核心职责：
///   1. 读取稳定实例标识，并结合 ClientCode 进行云端寻址
///   2. 心跳循环：定时调云端寻址接口探测网络状态
///   3. 维护 Online / Offline 状态，切换时发布事件
///   4. 本地文件缓存 DeviceSession，断网时可用
/// 
/// 心跳策略：
///   Online  → 1 分钟一次
///   Offline → 10 秒一次
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly HttpClient _httpClient;
    private readonly ICloudApiEndpointProvider _endpointProvider;
    private readonly IDeviceInstanceIdResolver _instanceIdResolver;
    private readonly ILogService _logger;
    private readonly object _stateLock = new();

    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;

    private static readonly TimeSpan OnlineInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan OfflineInterval = TimeSpan.FromSeconds(10);

    private static readonly string CacheFile =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "device_cache.json");

    public DeviceSession? CurrentDevice { get; private set; }
    public NetworkState CurrentState { get; private set; } = NetworkState.Offline;
    public bool HasDeviceId => CurrentDevice is not null;

    public event Action<NetworkState>? NetworkStateChanged;
    public event Action<DeviceSession?>? DeviceIdentified;

    public DeviceService(
        HttpClient httpClient,
        ICloudApiEndpointProvider endpointProvider,
        IDeviceInstanceIdResolver instanceIdResolver,
        ILogService logger)
    {
        _httpClient = httpClient;
        _endpointProvider = endpointProvider;
        _instanceIdResolver = instanceIdResolver;
        _logger = logger;
    }

    // ── 启动 / 停止 ─────────────────────────────────────────────

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
                try { await _heartbeatTask; }
                catch (OperationCanceledException) { }
            }

            _cts.Dispose();
            _cts = null;
        }
    }

    // ── 心跳循环 ─────────────────────────────────────────────────

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        _logger.Info("[DeviceService] 心跳循环启动");

        // 启动时立即执行一次寻址
        await IdentifyOnceAsync();

        while (!ct.IsCancellationRequested)
        {
            var interval = CurrentState == NetworkState.Online
                ? OnlineInterval
                : OfflineInterval;

            try
            {
                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await IdentifyOnceAsync();
        }

        _logger.Info("[DeviceService] 心跳循环停止");
    }

    // ── 单次寻址 ─────────────────────────────────────────────────

    private async Task IdentifyOnceAsync()
    {
        var instanceId = _instanceIdResolver.ResolveInstanceId();

        try
        {
            var clientCode = _endpointProvider.GetClientCode();
            var deviceInstancePath = _endpointProvider.GetDeviceInstancePath();
            var url = _endpointProvider.BuildUrl(
                $"{deviceInstancePath}?macAddress={Uri.EscapeDataString(instanceId)}&clientCode={Uri.EscapeDataString(clientCode)}");

            var response = await _httpClient
                .GetAsync(url)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn($"[DeviceService] 云端寻址失败: {response.StatusCode}");
                GoOffline(instanceId);
                return;
            }

            var dto = await response.Content
                .ReadFromJsonAsync<DeviceResponseDto>()
                .ConfigureAwait(false);

            if (dto is null)
            {
                _logger.Warn("[DeviceService] 云端返回数据为空");
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
            _logger.Warn("[DeviceService] 云端寻址超时");
            GoOffline(instanceId);
        }
        catch (HttpRequestException ex)
        {
            _logger.Warn($"[DeviceService] 网络异常: {ex.Message}");
            GoOffline(instanceId);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DeviceService] 寻址异常: {ex.Message}");
            GoOffline(instanceId);
        }
    }

    // ── 状态切换 ─────────────────────────────────────────────────

    private void GoOnline(DeviceSession session)
    {
        lock (_stateLock)
        {
            var deviceChanged = CurrentDevice is null
                || CurrentDevice.DeviceId != session.DeviceId
                || CurrentDevice.DeviceName != session.DeviceName
                || CurrentDevice.ProcessId != session.ProcessId;

            CurrentDevice = session;
            SaveToCache(session);

            if (deviceChanged)
            {
                _logger.Info($"[DeviceService] 设备信息更新: {session.DeviceName}");
                DeviceIdentified?.Invoke(CurrentDevice);
            }

            if (CurrentState != NetworkState.Online)
            {
                CurrentState = NetworkState.Online;
                _logger.Info("[DeviceService] 状态切换 → Online");
                NetworkStateChanged?.Invoke(NetworkState.Online);
            }
        }
    }

    private void GoOffline(string instanceId)
    {
        lock (_stateLock)
        {
            // 首次离线且没有 DeviceSession，尝试读缓存
            if (CurrentDevice is null)
            {
                TryLoadFromCache(instanceId);
            }

            if (CurrentState != NetworkState.Offline)
            {
                CurrentState = NetworkState.Offline;
                _logger.Info("[DeviceService] 状态切换 → Offline");
                NetworkStateChanged?.Invoke(NetworkState.Offline);
            }
        }
    }

    // ── 本地缓存 ─────────────────────────────────────────────────

    private void SaveToCache(DeviceSession session)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(session);
            File.WriteAllText(CacheFile, json);
        }
        catch (Exception ex)
        {
            _logger.Warn($"[DeviceService] 缓存写入失败: {ex.Message}");
        }
    }

    private void TryLoadFromCache(string instanceId)
    {
        try
        {
            if (!File.Exists(CacheFile)) return;

            var json = File.ReadAllText(CacheFile);
            var session = System.Text.Json.JsonSerializer.Deserialize<DeviceSession>(json);
            if (session is null) return;

            // 用缓存数据，实例标识用当前解析值
            CurrentDevice = session with { MacAddress = instanceId };
            _logger.Info($"[DeviceService] 加载本地缓存: {session.DeviceName}");
            DeviceIdentified?.Invoke(CurrentDevice);
        }
        catch (Exception ex)
        {
            _logger.Warn($"[DeviceService] 缓存读取失败: {ex.Message}");
        }
    }
}

