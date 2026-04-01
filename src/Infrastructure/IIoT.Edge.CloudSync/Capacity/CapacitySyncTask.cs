using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.CloudSync.Capacity;

/// <summary>
/// 产能定时同步任务
/// 
/// 60 秒间隔
/// 实时上传：在线时 POST 每台设备的当天产能快照
/// 失败 → 不处理（CapacityConsumer 离线时已写缓冲，RetryTask 补传）
/// 
/// RetryBufferAsync：由 RetryTask 调用，补传 SQLite 中积压的产能缓冲
/// </summary>
public class CapacitySyncTask : ICapacitySyncTask
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly IDeviceService _deviceService;
    private readonly IProductionContextStore _contextStore;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService _logger;
    private readonly ShiftConfig _shiftConfig;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    public CapacitySyncTask(
        ICloudHttpClient cloudHttp,
        IDeviceService deviceService,
        IProductionContextStore contextStore,
        ICapacityBufferStore bufferStore,
        ILogService logger,
        ShiftConfig shiftConfig)
    {
        _cloudHttp = cloudHttp;
        _deviceService = deviceService;
        _contextStore = contextStore;
        _bufferStore = bufferStore;
        _logger = logger;
        _shiftConfig = shiftConfig;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = Task.Run(() => SyncLoopAsync(_cts.Token), _cts.Token);
        _logger.Info("[CapacitySync] 产能同步启动，间隔 60 秒");
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
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(SyncInterval, ct); }
            catch (OperationCanceledException) { break; }

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
            await SyncAllDevicesAsync(device.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.Error($"[CapacitySync] 同步异常: {ex.Message}");
        }
    }

    private async Task SyncAllDevicesAsync(Guid cloudDeviceId)
    {
        var contexts = _contextStore.GetAll();

        foreach (var ctx in contexts)
        {
            var capacity = ctx.TodayCapacity;

            if (string.IsNullOrEmpty(capacity.Date) || capacity.TotalAll == 0)
                continue;

            foreach (var slot in capacity.HalfHourly.Where(h => h.Total > 0).OrderBy(h => h.SlotIndex))
            {
                var shiftCode = GetShiftCodeByTime(slot.StartHour, slot.StartMinute);
                await PostHalfHourCapacityAsync(
                    cloudDeviceId,
                    capacity.Date,
                    slot.StartHour,
                    slot.StartMinute,
                    shiftCode,
                    slot.Total,
                    slot.OkCount,
                    slot.NgCount,
                    ctx.DeviceName);
            }
        }
    }

    private async Task PostHalfHourCapacityAsync(
        Guid deviceId, string date, int hour, int minute, string shiftCode,
        int totalCount, int okCount, int ngCount,
        string logLabel)
    {
        var endMinute = minute == 30 ? 0 : 30;
        var endHour = minute == 30 ? (hour + 1) % 24 : hour;

        var payload = new
        {
            deviceId,
            date,
            hour,
            minute,
            timeLabel = $"{hour:D2}:{minute:D2}-{endHour:D2}:{endMinute:D2}",
            shiftCode,
            totalCount,
            okCount,
            ngCount
        };

        var success = await _cloudHttp.PostAsync("/api/v1/Capacity/hourly", payload);

        if (success)
            _logger.Info($"[CapacitySync] [{logLabel}] {date} {hour:D2}:{minute:D2}/{shiftCode} 同步成功: 总={totalCount}, OK={okCount}, NG={ngCount}");
        else
            _logger.Warn($"[CapacitySync] [{logLabel}] {date} {hour:D2}:{minute:D2}/{shiftCode} 同步失败");
    }

    // ── RetryTask 调用：补传离线缓冲 ─────────────────────────

    public async Task<bool> RetryBufferAsync()
    {
        var summaries = await _bufferStore.GetHourlySummaryAsync().ConfigureAwait(false);
        if (summaries.Count == 0)
            return true;

        var device = _deviceService.CurrentDevice;
        if (device is null) return false;

        foreach (var s in summaries)
        {
            var endMinute = s.MinuteBucket == 30 ? 0 : 30;
            var endHour = s.MinuteBucket == 30 ? (s.Hour + 1) % 24 : s.Hour;

            var payload = new
            {
                deviceId = device.DeviceId,
                date = s.Date,
                hour = s.Hour,
                minute = s.MinuteBucket,
                timeLabel = $"{s.Hour:D2}:{s.MinuteBucket:D2}-{endHour:D2}:{endMinute:D2}",
                shiftCode = s.ShiftCode,
                totalCount = s.Total,
                okCount = s.OkCount,
                ngCount = s.NgCount
            };

            var success = await _cloudHttp.PostAsync("/api/v1/Capacity/hourly", payload);
            if (!success)
            {
                _logger.Warn($"[重传-Cloud] 产能半小时补传失败 {s.Date} {s.Hour:D2}:{s.MinuteBucket:D2}/{s.ShiftCode}");
                return false;
            }
        }

        await _bufferStore.ClearAllAsync().ConfigureAwait(false);
        _logger.Info($"[重传-Cloud] 产能缓冲半小时补传完成，已清空 {summaries.Count} 条汇总");
        return true;
    }

    private string GetShiftCodeByTime(int hour, int minute)
    {
        var t = new TimeSpan(hour, minute, 0);
        var isDay = t >= _shiftConfig.DayStartTime && t < _shiftConfig.DayEndTime;
        return isDay ? "D" : "N";
    }
}