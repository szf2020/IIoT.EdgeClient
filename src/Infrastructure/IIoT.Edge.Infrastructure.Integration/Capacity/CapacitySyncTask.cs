using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Infrastructure.Integration.Config;
using IIoT.Edge.SharedKernel.DataPipeline.Capacity;

namespace IIoT.Edge.Infrastructure.Integration.Capacity;

public class CapacitySyncTask : ICapacitySyncTask
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly ICloudApiEndpointProvider _endpointProvider;
    private readonly IDeviceService _deviceService;
    private readonly IProductionContextStore _contextStore;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService _logger;
    private readonly ShiftConfig _shiftConfig;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    public CapacitySyncTask(ICloudHttpClient cloudHttp, ICloudApiEndpointProvider endpointProvider, IDeviceService deviceService, IProductionContextStore contextStore, ICapacityBufferStore bufferStore, ILogService logger, ShiftConfig shiftConfig)
    {
        _cloudHttp = cloudHttp;
        _endpointProvider = endpointProvider;
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
        _logger.Info("[CapacitySync] Started. Interval: 60s");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_loopTask is not null)
            {
                try { await _loopTask; } catch (OperationCanceledException) { }
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
        _logger.Info("[CapacitySync] Stopped.");
    }

    private async Task ExecuteOnceAsync()
    {
        if (_deviceService.CurrentState == NetworkState.Offline) return;
        var device = _deviceService.CurrentDevice;
        if (device is null) return;

        try { await SyncAllDevicesAsync(device.DeviceId); }
        catch (Exception ex) { _logger.Error($"[CapacitySync] Sync failed: {ex.Message}"); }
    }

    private async Task SyncAllDevicesAsync(Guid cloudDeviceId)
    {
        var contexts = _contextStore.GetAll();
        foreach (var ctx in contexts)
        {
            var capacity = ctx.TodayCapacity;
            if (string.IsNullOrEmpty(capacity.Date) || capacity.TotalAll == 0) continue;

            foreach (var slot in capacity.HalfHourly.Where(h => h.Total > 0).OrderBy(h => h.SlotIndex))
            {
                var shiftCode = GetShiftCodeByTime(slot.StartHour, slot.StartMinute);
                await PostHalfHourCapacityAsync(cloudDeviceId, capacity.Date, slot.StartHour, slot.StartMinute, shiftCode, slot.Total, slot.OkCount, slot.NgCount, ctx.DeviceName);
            }
        }
    }

    private async Task PostHalfHourCapacityAsync(Guid deviceId, string date, int hour, int minute, string shiftCode, int totalCount, int okCount, int ngCount, string plcName)
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
            ngCount,
            plcName
        };

        var success = await _cloudHttp.PostAsync(_endpointProvider.GetCapacityHourlyPath(), payload);
        if (success)
            _logger.Info($"[CapacitySync] [{plcName}] {date} {hour:D2}:{minute:D2}/{shiftCode} synced. Total:{totalCount}, OK:{okCount}, NG:{ngCount}");
        else
            _logger.Warn($"[CapacitySync] [{plcName}] {date} {hour:D2}:{minute:D2}/{shiftCode} sync failed.");
    }

    public async Task<bool> RetryBufferAsync()
    {
        var summaries = await _bufferStore.GetHourlySummaryAsync().ConfigureAwait(false);
        if (summaries.Count == 0) return true;

        var device = _deviceService.CurrentDevice;
        if (device is null) return false;

        foreach (var summary in summaries)
        {
            var endMinute = summary.MinuteBucket == 30 ? 0 : 30;
            var endHour = summary.MinuteBucket == 30 ? (summary.Hour + 1) % 24 : summary.Hour;
            var payload = new
            {
                deviceId = device.DeviceId,
                date = summary.Date,
                hour = summary.Hour,
                minute = summary.MinuteBucket,
                timeLabel = $"{summary.Hour:D2}:{summary.MinuteBucket:D2}-{endHour:D2}:{endMinute:D2}",
                shiftCode = summary.ShiftCode,
                totalCount = summary.Total,
                okCount = summary.OkCount,
                ngCount = summary.NgCount,
                plcName = summary.PlcName
            };

            var success = await _cloudHttp.PostAsync(_endpointProvider.GetCapacityHourlyPath(), payload);
            if (!success)
            {
                _logger.Warn($"[Retry-Cloud] Capacity retry failed: {summary.Date} {summary.Hour:D2}:{summary.MinuteBucket:D2}/{summary.ShiftCode}");
                return false;
            }
        }

        await _bufferStore.ClearAllAsync().ConfigureAwait(false);
        _logger.Info($"[Retry-Cloud] Capacity retry completed. Cleared {summaries.Count} summary row(s).");
        return true;
    }

    private string GetShiftCodeByTime(int hour, int minute)
    {
        var time = new TimeSpan(hour, minute, 0);
        var isDay = time >= _shiftConfig.DayStartTime && time < _shiftConfig.DayEndTime;
        return isDay ? "D" : "N";
    }
}
