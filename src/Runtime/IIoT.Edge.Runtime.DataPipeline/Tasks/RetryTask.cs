using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Consumers;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Runtime.Base;
using IIoT.Edge.SharedKernel.DataPipeline;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;
using System.Text.Json;

namespace IIoT.Edge.Runtime.DataPipeline.Tasks;

public class RetryTask : ScheduledTaskBase
{
    private readonly string _channel;
    private readonly IFailedRecordStore _failedStore;
    private readonly IDeviceService _deviceService;
    private readonly List<ICellDataConsumer> _consumers;
    private readonly ICloudBatchConsumer? _cloudBatchConsumer;
    private readonly IDeviceLogSyncTask? _deviceLogSync;
    private readonly ICapacitySyncTask? _capacitySync;
    private bool _wasOffline = true;

    private const int MaxRetryCount = 20;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public override string TaskName => $"RetryTask[{_channel}]";
    protected override int ExecuteInterval => 5000;

    public RetryTask(
        string channel,
        ILogService logger,
        IFailedRecordStore failedStore,
        IDeviceService deviceService,
        IEnumerable<ICellDataConsumer> consumers,
        IDeviceLogSyncTask? deviceLogSync = null,
        ICapacitySyncTask? capacitySync = null,
        ICloudBatchConsumer? cloudBatchConsumer = null)
        : base(logger)
    {
        _channel = channel;
        _failedStore = failedStore;
        _deviceService = deviceService;
        _consumers = consumers.OrderBy(c => c.Order).ToList();
        _deviceLogSync = deviceLogSync;
        _capacitySync = capacitySync;
        _cloudBatchConsumer = cloudBatchConsumer;
    }

    protected override async Task ExecuteAsync()
    {
        if (_channel == "Cloud")
        {
            var cloudReady = _deviceService.CurrentState == NetworkState.Online && _deviceService.HasDeviceId;
            if (!cloudReady)
            {
                _wasOffline = true;
                return;
            }

            if (_wasOffline)
            {
                _wasOffline = false;
                await RecoverAbandonedRecordsAsync();
            }
        }

        await RetryFailedCellRecordsAsync();

        if (_channel == "Cloud" && _deviceLogSync is not null)
        {
            var retried = await _deviceLogSync.RetryBufferAsync();
            if (!retried)
            {
                Logger.Warn($"[Retry-{_channel}] Device log buffer retry did not fully succeed.");
            }
        }

        if (_channel == "Cloud" && _capacitySync is not null)
        {
            var retried = await _capacitySync.RetryBufferAsync();
            if (!retried)
            {
                Logger.Warn($"[Retry-{_channel}] Capacity buffer retry did not fully succeed.");
            }
        }
    }

    private async Task RecoverAbandonedRecordsAsync()
    {
        try
        {
            await _failedStore.ResetAllAbandonedAsync().ConfigureAwait(false);
            Logger.Info($"[Retry-{_channel}] Network recovered. Abandoned records were reset for retry.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Retry-{_channel}] Failed to reset abandoned records: {ex.Message}");
        }
    }

    private async Task RetryFailedCellRecordsAsync()
    {
        if (_channel == "Cloud" && _cloudBatchConsumer is not null)
        {
            await RetryCloudInjectionBatchesAsync();
            return;
        }

        var records = await _failedStore.GetPendingAsync(_channel, batchSize: 5).ConfigureAwait(false);
        if (records.Count == 0)
        {
            return;
        }

        foreach (var record in records)
        {
            await ProcessOneAsync(record).ConfigureAwait(false);
        }
    }

    private async Task RetryCloudInjectionBatchesAsync()
    {
        var records = await _failedStore.GetPendingAsync(_channel, batchSize: 100).ConfigureAwait(false);
        if (records.Count == 0)
        {
            return;
        }

        var batchCandidates = records
            .Where(r => r.ProcessType == "Injection" && r.FailedTarget == "Cloud")
            .ToList();

        var others = records
            .Where(r => !(r.ProcessType == "Injection" && r.FailedTarget == "Cloud"))
            .ToList();

        foreach (var chunk in batchCandidates.Chunk(100))
        {
            var completedRecords = new List<CellCompletedRecord>();
            var validSourceRecords = new List<FailedCellRecord>();

            foreach (var source in chunk)
            {
                var cellData = DeserializeCellData(source.ProcessType, source.CellDataJson);
                if (cellData is null)
                {
                    Logger.Error($"[Retry-{_channel}] Deserialize failed for process type {source.ProcessType}. Delete record.");
                    await _failedStore.DeleteAsync(source.Id).ConfigureAwait(false);
                    continue;
                }

                completedRecords.Add(new CellCompletedRecord { CellData = cellData });
                validSourceRecords.Add(source);
            }

            if (completedRecords.Count == 0)
            {
                continue;
            }

            var success = await _cloudBatchConsumer!.ProcessBatchAsync(completedRecords).ConfigureAwait(false);
            if (success)
            {
                foreach (var source in validSourceRecords)
                {
                    await _failedStore.DeleteAsync(source.Id).ConfigureAwait(false);
                }

                Logger.Info($"[Retry-{_channel}] Injection batch retry succeeded. Count:{validSourceRecords.Count}");
                continue;
            }

            foreach (var source in validSourceRecords)
            {
                await HandleRetryFailureAsync(source, source.FailedTarget, "Cloud batch retry failed.").ConfigureAwait(false);
            }

            Logger.Warn($"[Retry-{_channel}] Injection batch retry failed. Count:{validSourceRecords.Count}");
            break;
        }

        foreach (var record in others)
        {
            await ProcessOneAsync(record).ConfigureAwait(false);
        }
    }

    private async Task ProcessOneAsync(FailedCellRecord record)
    {
        var startIndex = _consumers.FindIndex(c => c.Name == record.FailedTarget);
        if (startIndex < 0)
        {
            Logger.Warn($"[Retry-{_channel}] Consumer {record.FailedTarget} was not found. Delete record.");
            await _failedStore.DeleteAsync(record.Id);
            return;
        }

        var cellData = DeserializeCellData(record.ProcessType, record.CellDataJson);
        if (cellData is null)
        {
            Logger.Error($"[Retry-{_channel}] Deserialize failed for process type {record.ProcessType}. Delete record.");
            await _failedStore.DeleteAsync(record.Id);
            return;
        }

        var completedRecord = new CellCompletedRecord { CellData = cellData };
        var label = cellData.DisplayLabel;

        for (var i = startIndex; i < _consumers.Count; i++)
        {
            var consumer = _consumers[i];
            if (consumer.RetryChannel != _channel)
            {
                continue;
            }

            try
            {
                var success = await consumer.ProcessAsync(completedRecord).ConfigureAwait(false);
                if (!success)
                {
                    Logger.Warn($"[Retry-{_channel}] {label} still failed at {consumer.Name}.");
                    await HandleRetryFailureAsync(record, consumer.Name, "Consumer returned false.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Retry-{_channel}] {label} failed at {consumer.Name}: {ex.Message}");
                await HandleRetryFailureAsync(record, consumer.Name, ex.Message);
                return;
            }
        }

        await _failedStore.DeleteAsync(record.Id);
        Logger.Info($"[Retry-{_channel}] {label} retry succeeded and the record was removed.");
    }

    private async Task HandleRetryFailureAsync(FailedCellRecord record, string failedTarget, string errorMessage)
    {
        var newRetryCount = record.RetryCount + 1;

        if (newRetryCount > MaxRetryCount)
        {
            Logger.Warn($"[Retry-{_channel}] {record.ProcessType} reached max retry count {MaxRetryCount}. Auto retry stopped.");
            await _failedStore.UpdateRetryAsync(record.Id, newRetryCount, errorMessage, DateTime.MaxValue);
            return;
        }

        var nextRetryTime = DateTime.Now.Add(CalculateBackoff(newRetryCount));
        await _failedStore.UpdateRetryAsync(record.Id, newRetryCount, errorMessage, nextRetryTime);
    }

    private static TimeSpan CalculateBackoff(int retryCount)
    {
        if (retryCount <= 5) return TimeSpan.FromSeconds(30);
        if (retryCount <= 10) return TimeSpan.FromMinutes(5);
        return TimeSpan.FromMinutes(30);
    }

    private CellDataBase? DeserializeCellData(string processType, string json)
    {
        try
        {
            return CellDataTypeRegistry.Deserialize(processType, json, _jsonOptions);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Retry-{_channel}] CellData deserialize failed: {ex.Message}");
            return null;
        }
    }
}
