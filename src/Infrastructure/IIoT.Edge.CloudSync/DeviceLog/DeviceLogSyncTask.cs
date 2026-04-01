using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.DeviceLog;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;
using System.Collections.Concurrent;

namespace IIoT.Edge.CloudSync.DeviceLog;

/// <summary>
/// 设备日志定时同步任务
/// 
/// 订阅 ILogService.EntryAdded → 全级别入内存队列
/// 60 秒定时 Drain：
///   在线 → 批量 POST /api/v1/DeviceLog
///   POST 失败 → 写 SQLite 缓冲（RetryTask 补传）
///   离线 → 写 SQLite 缓冲
/// 
/// RetryBufferAsync：由 RetryTask 调用，分批补传 SQLite 缓冲
/// </summary>
public class DeviceLogSyncTask : IDeviceLogSyncTask
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly IDeviceService _deviceService;
    private readonly IDeviceLogBufferStore _bufferStore;
    private readonly ILogService _logger;

    private readonly ConcurrentQueue<LogItem> _queue = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private const int RetryBatchSize = 100;
    private const int RetryMaxBatchesPerRound = 3;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    public DeviceLogSyncTask(
        ICloudHttpClient cloudHttp,
        IDeviceService deviceService,
        IDeviceLogBufferStore bufferStore,
        ILogService logger)
    {
        _cloudHttp = cloudHttp;
        _deviceService = deviceService;
        _bufferStore = bufferStore;
        _logger = logger;

        // 订阅全级别日志
        _logger.EntryAdded += OnLogEntryAdded;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = Task.Run(() => SyncLoopAsync(_cts.Token), _cts.Token);
        _logger.Info("[DeviceLogSync] 日志同步启动，间隔 60 秒");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _logger.EntryAdded -= OnLogEntryAdded;

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

        // 停止前把内存残留写入 SQLite
        await FlushQueueToBufferAsync();
    }

    private void OnLogEntryAdded(IIoT.Edge.Contracts.Model.LogEntry entry)
    {
        _queue.Enqueue(new LogItem
        {
            Level = entry.Level,
            Message = entry.Message,
            LogTime = entry.Time
        });
    }

    private async Task SyncLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(SyncInterval, ct); }
            catch (OperationCanceledException) { break; }

            await ExecuteOnceAsync();
        }
    }

    private async Task ExecuteOnceAsync()
    {
        var batch = DrainQueue();
        if (batch.Count == 0) return;

        var device = _deviceService.CurrentDevice;

        // 离线或无 DeviceId → 直接存 SQLite
        if (device is null || _deviceService.CurrentState == NetworkState.Offline)
        {
            await SaveToBufferAsync(batch);
            return;
        }

        // 在线 → POST 云端
        var success = await PostLogsAsync(device.DeviceId, batch);
        if (!success)
        {
            await SaveToBufferAsync(batch);
        }
    }

    private async Task<bool> PostLogsAsync(Guid deviceId, List<LogItem> batch)
    {
        var payload = new
        {
            logs = batch.Select(l => new
            {
                deviceId,
                level = l.Level,
                message = l.Message,
                logTime = l.LogTime.ToString("O")
            }).ToArray()
        };

        // 用 PostAsync，内部已捕获异常
        // 注意：不用 _logger 记录失败，避免死循环（日志失败→产生日志→入队列）
        return await _cloudHttp.PostAsync("/api/v1/DeviceLog", payload);
    }

    private async Task SaveToBufferAsync(List<LogItem> batch)
    {
        var records = batch.Select(l => new DeviceLogRecord
        {
            Level = l.Level,
            Message = l.Message,
            LogTime = l.LogTime.ToString("O"),
            CreatedAt = DateTime.Now.ToString("O")
        });

        await _bufferStore.SaveBatchAsync(records);
    }

    private async Task FlushQueueToBufferAsync()
    {
        var remaining = DrainQueue();
        if (remaining.Count > 0)
            await SaveToBufferAsync(remaining);
    }

    private List<LogItem> DrainQueue()
    {
        var list = new List<LogItem>();
        while (_queue.TryDequeue(out var item))
            list.Add(item);
        return list;
    }

    // ── RetryTask 调用：分批补传 SQLite 缓冲 ─────────────────

    public async Task<bool> RetryBufferAsync()
    {
        var device = _deviceService.CurrentDevice;
        if (device is null) return false;

        for (int i = 0; i < RetryMaxBatchesPerRound; i++)
        {
            var records = await _bufferStore
                .GetPendingAsync(RetryBatchSize)
                .ConfigureAwait(false);

            if (records.Count == 0)
                return true; // 全部补传完了

            var payload = new
            {
                logs = records.Select(r => new
                {
                    deviceId = device.DeviceId,
                    level = r.Level,
                    message = r.Message,
                    logTime = r.LogTime
                }).ToArray()
            };

            var success = await _cloudHttp.PostAsync("/api/v1/DeviceLog", payload);
            if (!success)
                return false; // 这批失败，下轮再来

            await _bufferStore
                .DeleteBatchAsync(records.Select(r => r.Id))
                .ConfigureAwait(false);

            if (records.Count < RetryBatchSize)
                return true; // 不满一批，说明没有更多了
        }

        return true;
    }

    private class LogItem
    {
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime LogTime { get; set; }
    }
}