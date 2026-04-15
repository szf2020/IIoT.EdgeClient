using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Common.Models;
using IIoT.Edge.Infrastructure.Integration.Config;
using IIoT.Edge.SharedKernel.DataPipeline.DeviceLog;
using System.Collections.Concurrent;

namespace IIoT.Edge.Infrastructure.Integration.DeviceLog;

public class DeviceLogSyncTask : IDeviceLogSyncTask
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly ICloudApiEndpointProvider _endpointProvider;
    private readonly IDeviceService _deviceService;
    private readonly IDeviceLogBufferStore _bufferStore;
    private readonly ILogService _logger;
    private readonly ConcurrentQueue<LogItem> _queue = new();
    private readonly object _lifecycleLock = new();
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _isRunning;
    private bool _isSubscribed;
    private const int RetryBatchSize = 100;
    private const int RetryMaxBatchesPerRound = 3;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    public DeviceLogSyncTask(ICloudHttpClient cloudHttp, ICloudApiEndpointProvider endpointProvider, IDeviceService deviceService, IDeviceLogBufferStore bufferStore, ILogService logger)
    {
        _cloudHttp = cloudHttp;
        _endpointProvider = endpointProvider;
        _deviceService = deviceService;
        _bufferStore = bufferStore;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        lock (_lifecycleLock)
        {
            if (_isRunning)
            {
                return Task.CompletedTask;
            }

            if (!_isSubscribed)
            {
                _logger.EntryAdded += OnLogEntryAdded;
                _isSubscribed = true;
            }

            _isRunning = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loopTask = Task.Run(() => SyncLoopAsync(_cts.Token), CancellationToken.None);
        }

        _logger.Info("[DeviceLogSync] Started. Interval: 60s");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? localCts;
        Task? localLoopTask;

        lock (_lifecycleLock)
        {
            if (!_isRunning && !_isSubscribed)
            {
                return;
            }

            _isRunning = false;
            localCts = _cts;
            localLoopTask = _loopTask;
            _cts = null;
            _loopTask = null;

            if (_isSubscribed)
            {
                _logger.EntryAdded -= OnLogEntryAdded;
                _isSubscribed = false;
            }
        }

        if (localCts is not null)
        {
            await localCts.CancelAsync();
            if (localLoopTask is not null)
            {
                try
                {
                    await localLoopTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
            localCts.Dispose();
        }

        await FlushQueueToBufferAsync();
        _logger.Info("[DeviceLogSync] Stopped.");
    }

    private void OnLogEntryAdded(LogEntry entry)
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
        await _syncGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var batch = DrainQueue();
            if (batch.Count == 0) return;

            var device = _deviceService.CurrentDevice;
            if (device is null || _deviceService.CurrentState == NetworkState.Offline)
            {
                await SaveToBufferAsync(batch);
                return;
            }

            var success = await PostLogsAsync(device.DeviceId, batch);
            if (!success) await SaveToBufferAsync(batch);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task<bool> PostLogsAsync(Guid deviceId, List<LogItem> batch)
    {
        var payload = new
        {
            deviceId,
            logs = batch.Select(l => new
            {
                level = l.Level,
                message = l.Message,
                logTime = l.LogTime.ToString("O")
            }).ToArray()
        };

        return await _cloudHttp.PostAsync(_endpointProvider.GetDeviceLogPath(), payload);
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
        if (remaining.Count > 0) await SaveToBufferAsync(remaining);
    }

    private List<LogItem> DrainQueue()
    {
        var list = new List<LogItem>();
        while (_queue.TryDequeue(out var item)) list.Add(item);
        return list;
    }

    public async Task<bool> RetryBufferAsync()
    {
        await _syncGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_deviceService.CurrentState == NetworkState.Offline)
            {
                return false;
            }

            var device = _deviceService.CurrentDevice;
            if (device is null) return false;

            for (var i = 0; i < RetryMaxBatchesPerRound; i++)
            {
                var records = await _bufferStore.GetPendingAsync(RetryBatchSize).ConfigureAwait(false);
                if (records.Count == 0) return true;

                var payload = new
                {
                    deviceId = device.DeviceId,
                    logs = records.Select(r => new
                    {
                        level = r.Level,
                        message = r.Message,
                        logTime = r.LogTime
                    }).ToArray()
                };

                var success = await _cloudHttp.PostAsync(_endpointProvider.GetDeviceLogPath(), payload);
                if (!success) return false;

                await _bufferStore.DeleteBatchAsync(records.Select(r => r.Id)).ConfigureAwait(false);
                if (records.Count < RetryBatchSize) return true;
            }

            return true;
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private class LogItem
    {
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime LogTime { get; set; }
    }
}
