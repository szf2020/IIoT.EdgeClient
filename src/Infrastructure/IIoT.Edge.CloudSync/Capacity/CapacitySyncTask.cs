// CloudSync 层实现，自己写定时循环，不依赖 ScheduledTaskBase
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using System.Net.Http.Json;

public class CapacitySyncTask : ICapacitySyncTask
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ITodayCapacityStore _todayStore;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService _logger;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(30);

    public CapacitySyncTask(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        ITodayCapacityStore todayStore,
        ICapacityBufferStore bufferStore,
        ILogService logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _todayStore = todayStore;
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

            // 1. 同步内存快照
            await SyncSnapshotAsync(client, device.DeviceId);

            // 2. 补传离线缓冲
            await FlushBufferAsync(client, device.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.Error($"[CapacitySync] 同步异常: {ex.Message}");
        }
    }

    private async Task SyncSnapshotAsync(HttpClient client, Guid deviceId)
    {
        var snapshot = _todayStore.GetSnapshot();

        if (string.IsNullOrEmpty(snapshot.Date) || snapshot.TotalAll == 0)
            return;

        if (snapshot.DayShift.Total > 0)
        {
            await PostCapacityAsync(client, deviceId,
                snapshot.Date, "D",
                snapshot.DayShift.Total,
                snapshot.DayShift.OkCount,
                snapshot.DayShift.NgCount);
        }

        if (snapshot.NightShift.Total > 0)
        {
            await PostCapacityAsync(client, deviceId,
                snapshot.Date, "N",
                snapshot.NightShift.Total,
                snapshot.NightShift.OkCount,
                snapshot.NightShift.NgCount);
        }
    }

    private async Task FlushBufferAsync(HttpClient client, Guid deviceId)
    {
        var summaries = await _bufferStore.GetShiftSummaryAsync().ConfigureAwait(false);

        if (summaries.Count == 0)
            return;

        var allSuccess = true;
        foreach (var s in summaries)
        {
            var success = await PostCapacityAsync(client, deviceId,
                s.Date, s.ShiftCode, s.Total, s.OkCount, s.NgCount);

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
        int totalCount, int okCount, int ngCount)
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
                _logger.Info($"[CapacitySync] {date}/{shiftCode} 同步成功: " +
                    $"总={totalCount}, OK={okCount}, NG={ngCount}");
                return true;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.Warn($"[CapacitySync] {date}/{shiftCode} 同步失败: " +
                $"{response.StatusCode}, {body}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.Warn($"[CapacitySync] {date}/{shiftCode} 同步超时");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.Warn($"[CapacitySync] {date}/{shiftCode} 网络异常: {ex.Message}");
            return false;
        }
    }
}