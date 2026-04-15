using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Consumers;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Common.Models;
using IIoT.Edge.Infrastructure.Integration.Config;
using IIoT.Edge.SharedKernel.Context;
using IIoT.Edge.SharedKernel.DataPipeline.Capacity;
using IIoT.Edge.SharedKernel.DataPipeline;
using IIoT.Edge.SharedKernel.DataPipeline.DeviceLog;

namespace IIoT.Edge.NonUiRegressionTests;

internal sealed class FakeLogService : ILogService
{
    public List<LogEntry> Entries { get; } = new();

    public event Action<LogEntry>? EntryAdded;

    public void Debug(string message) => Write("Debug", message);
    public void Info(string message) => Write("Info", message);
    public void Warn(string message) => Write("Warn", message);
    public void Error(string message) => Write("Error", message);
    public void Fatal(string message) => Write("Fatal", message);

    private void Write(string level, string message)
    {
        var entry = new LogEntry
        {
            Time = DateTime.UtcNow,
            Level = level,
            Message = message
        };

        Entries.Add(entry);
        EntryAdded?.Invoke(entry);
    }
}

internal sealed class FakeDeviceService : IDeviceService
{
    public DeviceSession? CurrentDevice { get; set; }
    public NetworkState CurrentState { get; set; } = NetworkState.Offline;
    public bool HasDeviceId { get; set; }

    public event Action<NetworkState>? NetworkStateChanged;
    public event Action<DeviceSession?>? DeviceIdentified;

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;

    public void SetOnline(DeviceSession session)
    {
        CurrentDevice = session;
        CurrentState = NetworkState.Online;
        HasDeviceId = true;
        DeviceIdentified?.Invoke(session);
        NetworkStateChanged?.Invoke(CurrentState);
    }

    public void SetOffline()
    {
        CurrentState = NetworkState.Offline;
        HasDeviceId = false;
        NetworkStateChanged?.Invoke(CurrentState);
    }
}

internal sealed class FakeCellDataConsumer : ICellDataConsumer
{
    private readonly bool _result;

    public FakeCellDataConsumer(string name, int order, string? retryChannel, bool result)
    {
        Name = name;
        Order = order;
        RetryChannel = retryChannel;
        _result = result;
    }

    public string Name { get; }
    public int Order { get; }
    public string? RetryChannel { get; }

    public int ProcessCallCount { get; private set; }

    public Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        ProcessCallCount++;
        return Task.FromResult(_result);
    }
}

internal sealed class FakeFailedRecordStore : IFailedRecordStore
{
    public sealed record RetryUpdate(long Id, int RetryCount, string ErrorMessage, DateTime NextRetryTime);

    public List<FailedCellRecord> PendingRecords { get; } = new();
    public Dictionary<long, RetryUpdate> Updates { get; } = new();
    public List<long> DeletedIds { get; } = new();
    public int ResetAllAbandonedCallCount { get; private set; }

    public Task SaveAsync(CellCompletedRecord record, string failedTarget, string errorMessage, string channel)
    {
        PendingRecords.Add(new FailedCellRecord
        {
            Id = PendingRecords.Count + 1,
            Channel = channel,
            FailedTarget = failedTarget,
            ErrorMessage = errorMessage,
            ProcessType = record.CellData.ProcessType,
            CellDataJson = "{}",
            NextRetryTime = DateTime.Now
        });
        return Task.CompletedTask;
    }

    public Task<List<FailedCellRecord>> GetPendingAsync(string channel, int batchSize = 10)
    {
        var now = DateTime.Now;
        var rows = PendingRecords
            .Where(r => r.Channel == channel && r.NextRetryTime <= now)
            .OrderBy(r => r.Id)
            .Take(batchSize)
            .ToList();

        return Task.FromResult(rows);
    }

    public Task DeleteAsync(long id)
    {
        DeletedIds.Add(id);
        PendingRecords.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task UpdateRetryAsync(long id, int retryCount, string errorMessage, DateTime nextRetryTime)
    {
        Updates[id] = new RetryUpdate(id, retryCount, errorMessage, nextRetryTime);
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync() => Task.FromResult(PendingRecords.Count);

    public Task<int> GetCountAsync(string channel)
        => Task.FromResult(PendingRecords.Count(x => x.Channel == channel));

    public Task ResetAllAbandonedAsync()
    {
        ResetAllAbandonedCallCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeDeviceLogBufferStore : IDeviceLogBufferStore
{
    public List<DeviceLogRecord> Records { get; } = new();
    public List<long> DeletedIds { get; } = new();

    public Task SaveBatchAsync(IEnumerable<DeviceLogRecord> records)
    {
        var nextId = Records.Count == 0 ? 1 : Records.Max(x => x.Id) + 1;
        foreach (var record in records)
        {
            if (record.Id == 0)
            {
                record.Id = nextId++;
            }

            Records.Add(record);
        }

        return Task.CompletedTask;
    }

    public Task<List<DeviceLogRecord>> GetPendingAsync(int batchSize = 100)
    {
        var rows = Records
            .OrderBy(x => x.Id)
            .Take(batchSize)
            .ToList();

        return Task.FromResult(rows);
    }

    public Task DeleteBatchAsync(IEnumerable<long> ids)
    {
        var idList = ids.ToList();
        DeletedIds.AddRange(idList);
        Records.RemoveAll(x => idList.Contains(x.Id));
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync() => Task.FromResult(Records.Count);
}

internal sealed class FakeCloudHttpClient : ICloudHttpClient
{
    private readonly Queue<bool> _postResults = new();

    public int PostCallCount { get; private set; }
    public string? LastPostUrl { get; private set; }
    public object? LastPayload { get; private set; }
    public List<string> PostUrls { get; } = new();
    public List<object> PostPayloads { get; } = new();

    public void EnqueuePostResult(bool result) => _postResults.Enqueue(result);

    public Task<bool> PostAsync(string url, object payload)
    {
        PostCallCount++;
        LastPostUrl = url;
        LastPayload = payload;
        PostUrls.Add(url);
        PostPayloads.Add(payload);

        if (_postResults.Count > 0)
        {
            return Task.FromResult(_postResults.Dequeue());
        }

        return Task.FromResult(true);
    }

    public Task<string?> PostWithResponseAsync(string url, object payload)
        => Task.FromResult<string?>(null);

    public Task<string?> GetAsync(string url)
        => Task.FromResult<string?>(null);
}

internal sealed class FakeCloudApiEndpointProvider : ICloudApiEndpointProvider
{
    public string BuildUrl(string relativeOrAbsoluteUrl) => relativeOrAbsoluteUrl;
    public string GetClientCode() => "TEST";
    public string GetDeviceInstancePath() => "/device-instance";
    public string GetIdentityDeviceLoginPath() => "/device-login";
    public string GetPassStationInjectionBatchPath() => "/pass-station";
    public string GetDeviceLogPath() => "/device-log";
    public string BuildRecipeByDevicePath(Guid deviceId) => $"/recipe/{deviceId:N}";
    public string GetCapacityHourlyPath() => "/capacity-hourly";
    public string GetCapacitySummaryPath() => "/capacity-summary";
    public string GetCapacitySummaryRangePath() => "/capacity-summary-range";
}

internal sealed class FakeCapacityBufferStore : ICapacityBufferStore
{
    public List<CapacityRecord> Records { get; } = new();
    public List<BufferSummaryDto> ShiftSummaries { get; } = new();
    public List<BufferHourlySummaryDto> HourlySummaries { get; } = new();
    public int ClearAllCallCount { get; private set; }

    public Task SaveAsync(CapacityRecord record)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }

    public Task SaveBatchAsync(IEnumerable<CapacityRecord> records)
    {
        Records.AddRange(records);
        return Task.CompletedTask;
    }

    public Task<List<BufferSummaryDto>> GetShiftSummaryAsync()
        => Task.FromResult(ShiftSummaries.ToList());

    public Task<List<BufferHourlySummaryDto>> GetHourlySummaryAsync()
        => Task.FromResult(HourlySummaries.ToList());

    public Task ClearAllAsync()
    {
        ClearAllCallCount++;
        HourlySummaries.Clear();
        ShiftSummaries.Clear();
        Records.Clear();
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync()
        => Task.FromResult(Records.Count);
}

internal sealed class FakeProductionContextStore : IProductionContextStore
{
    private readonly Dictionary<string, ProductionContext> _contexts = new(StringComparer.OrdinalIgnoreCase);

    public ProductionContext GetOrCreate(string deviceName)
    {
        if (!_contexts.TryGetValue(deviceName, out var context))
        {
            context = new ProductionContext { DeviceName = deviceName };
            _contexts[deviceName] = context;
        }

        return context;
    }

    public IReadOnlyCollection<ProductionContext> GetAll() => _contexts.Values.ToList().AsReadOnly();

    public void LoadFromFile()
    {
    }

    public void SaveToFile()
    {
    }

    public Task StartAutoSaveAsync(CancellationToken ct, int intervalSeconds = 30) => Task.CompletedTask;
}

internal sealed class FakeCloudBatchConsumer : ICloudBatchConsumer
{
    private readonly Queue<bool> _results = new();

    public int ProcessBatchCallCount { get; private set; }
    public List<IReadOnlyList<CellCompletedRecord>> ReceivedBatches { get; } = new();

    public void EnqueueResult(bool result) => _results.Enqueue(result);

    public Task<bool> ProcessBatchAsync(IReadOnlyList<CellCompletedRecord> records)
    {
        ProcessBatchCallCount++;
        ReceivedBatches.Add(records.ToList());

        if (_results.Count > 0)
        {
            return Task.FromResult(_results.Dequeue());
        }

        return Task.FromResult(true);
    }
}

internal sealed class FakeDeviceLogSyncTask : IDeviceLogSyncTask
{
    public int RetryBufferCallCount { get; private set; }
    public bool RetryResult { get; set; } = true;

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;

    public Task<bool> RetryBufferAsync()
    {
        RetryBufferCallCount++;
        return Task.FromResult(RetryResult);
    }
}

internal sealed class FakeCapacitySyncTask : ICapacitySyncTask
{
    public int RetryBufferCallCount { get; private set; }
    public bool RetryResult { get; set; } = true;

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;

    public Task<bool> RetryBufferAsync()
    {
        RetryBufferCallCount++;
        return Task.FromResult(RetryResult);
    }
}
