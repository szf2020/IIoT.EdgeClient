using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using System.Net.Http.Json;

namespace IIoT.Edge.CloudSync.Capacity;

/// <summary>
/// 产能定时同步任务
/// 
/// 30 分钟间隔，对齐整点/半点
/// 
/// 两个数据源：
///   1. 遍历所有设备的 ProductionContext.TodayCapacity → 当天实时产能
///   2. CapacityBufferStore → 断网期间积压的记录
/// 
/// Online 时：POST 每台设备的白班+夜班产能 + 补传离线缓冲
/// Offline 时：跳过
/// </summary>
public class CapacitySyncTask : ICapacitySyncTask
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly IProductionContextStore _contextStore;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService _logger;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(30);

    public CapacitySyncTask(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        IProductionContextStore contextStore,
        ICapacityBufferStore bufferStore,
        ILogService logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _contextStore = contextStore;
        _bufferStore = bufferStore;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = Task.Run(() => SyncLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_loopTask is not null)
            {
                try { await _loopTask; }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task SyncLoopAsync(CancellationToken ct)
    {
        _logger.Info("[CapacitySync] 产能同步启动，间隔 30 分钟");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(SyncInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ExecuteOnceAsync();
        }

        _logger.Info("[CapacitySync] 产能同步已停止");
    }

    private async Task ExecuteOnceAsync()
    {
        if (_deviceService.CurrentState == NetworkState.Offline)
            return;

        var device = _deviceService.CurrentDevice;
        if (device is null)
            return;

        try
        {
            var client = _httpClientFactory.CreateClient("CloudApi");

            // 1. 遍历所有设备，同步各自的内存快照
            await SyncAllDevicesAsync(client, device.DeviceId);

            // 2. 补传离线缓冲
            await FlushBufferAsync(client, device.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.Error($"[CapacitySync] 同步异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 遍历所有 PLC 设备，逐台上传当天产能
    /// </summary>
    private async Task SyncAllDevicesAsync(HttpClient client, Guid cloudDeviceId)
    {
        var contexts = _contextStore.GetAll();

        foreach (var ctx in contexts)
        {
            var capacity = ctx.TodayCapacity;

            if (string.IsNullOrEmpty(capacity.Date) || capacity.TotalAll == 0)
                continue;

            if (capacity.DayShift.Total > 0)
            {
                await PostCapacityAsync(client, cloudDeviceId,
                    capacity.Date, "D",
                    capacity.DayShift.Total,
                    capacity.DayShift.OkCount,
                    capacity.DayShift.NgCount,
                    ctx.DeviceName);
            }

            if (capacity.NightShift.Total > 0)
            {
                await PostCapacityAsync(client, cloudDeviceId,
                    capacity.Date, "N",
                    capacity.NightShift.Total,
                    capacity.NightShift.OkCount,
                    capacity.NightShift.NgCount,
                    ctx.DeviceName);
            }
        }
    }

    /// <summary>
    /// 补传离线缓冲 → 按日期+班次汇总后 POST → 成功后清空
    /// </summary>
    private async Task FlushBufferAsync(HttpClient client, Guid cloudDeviceId)
    {
        var summaries = await _bufferStore.GetShiftSummaryAsync().ConfigureAwait(false);

        if (summaries.Count == 0)
            return;

        var allSuccess = true;
        foreach (var s in summaries)
        {
            var success = await PostCapacityAsync(client, cloudDeviceId,
                s.Date, s.ShiftCode, s.Total, s.OkCount, s.NgCount, "离线缓冲");

            if (!success)
            {
                allSuccess = false;
                break;
            }
        }

        if (allSuccess)
        {
            await _bufferStore.ClearAllAsync().ConfigureAwait(false);
            _logger.Info($"[CapacitySync] 离线缓冲补传完成，已清空 {summaries.Count} 条汇总");
        }
    }

    private async Task<bool> PostCapacityAsync(
        HttpClient client, Guid deviceId,
        string date, string shiftCode,
        int totalCount, int okCount, int ngCount,
        string logLabel)
    {
        var payload = new
        {
            deviceId,
            date,
            shiftCode,
            totalCount,
            okCount,
            ngCount
        };

        try
        {
            var response = await client
                .PostAsJsonAsync("/api/v1/Capacity/daily", payload)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.Info($"[CapacitySync] [{logLabel}] {date}/{shiftCode} 同步成功: " +
                    $"总={totalCount}, OK={okCount}, NG={ngCount}");
                return true;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.Warn($"[CapacitySync] [{logLabel}] {date}/{shiftCode} 同步失败: " +
                $"{response.StatusCode}, {body}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.Warn($"[CapacitySync] [{logLabel}] {date}/{shiftCode} 同步超时");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.Warn($"[CapacitySync] [{logLabel}] {date}/{shiftCode} 网络异常: {ex.Message}");
            return false;
        }
    }
}