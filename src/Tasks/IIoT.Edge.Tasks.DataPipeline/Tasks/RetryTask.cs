using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Tasks.Base;
using System.Text.Json;

namespace IIoT.Edge.Tasks.DataPipeline.Tasks;

/// <summary>
/// 云端重传调度中心
/// 
/// 按 channel 实例化，Cloud 通道串行调度三类补传：
///   ① 失败的生产数据（消费链重试）
///   ② 积压的设备日志（委托 IDeviceLogSyncTask.RetryBufferAsync）
///   ③ 积压的产能缓冲（委托 ICapacitySyncTask.RetryBufferAsync）
/// 
/// 自身不做 HTTP 请求，只负责调度顺序和在线判断
/// </summary>
public class RetryTask : ScheduledTaskBase
{
    private readonly string _channel;
    private readonly IFailedRecordStore _failedStore;
    private readonly IDeviceService _deviceService;
    private readonly List<ICellDataConsumer> _consumers;
    private readonly ICloudBatchConsumer? _cloudBatchConsumer;

    // Cloud 通道专用：日志和产能补传委托
    private readonly IDeviceLogSyncTask? _deviceLogSync;
    private readonly ICapacitySyncTask? _capacitySync;

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
        // Cloud 通道：Offline 或无 DeviceId 时跳过
        if (_channel == "Cloud" &&
            (_deviceService.CurrentState == NetworkState.Offline || !_deviceService.HasDeviceId))
            return;

        // ① 重传失败的生产数据
        await RetryFailedCellRecordsAsync();

        // ② 重传积压的设备日志（仅 Cloud 通道）
        if (_channel == "Cloud" && _deviceLogSync is not null)
            await _deviceLogSync.RetryBufferAsync();

        // ③ 重传积压的产能缓冲（仅 Cloud 通道）
        if (_channel == "Cloud" && _capacitySync is not null)
            await _capacitySync.RetryBufferAsync();
    }

    // ══════════════════════════════════════════════════════
    //  ① 生产数据重传（消费链重试）
    // ══════════════════════════════════════════════════════

    private async Task RetryFailedCellRecordsAsync()
    {
        if (_channel == "Cloud" && _cloudBatchConsumer is not null)
        {
            await RetryCloudInjectionBatchesAsync();
            return;
        }

        var records = await _failedStore
            .GetPendingAsync(_channel, batchSize: 5)
            .ConfigureAwait(false);

        if (records.Count == 0)
            return;

        foreach (var record in records)
        {
            await ProcessOneAsync(record).ConfigureAwait(false);
        }
    }

    private async Task RetryCloudInjectionBatchesAsync()
    {
        var records = await _failedStore
            .GetPendingAsync(_channel, batchSize: 100)
            .ConfigureAwait(false);

        if (records.Count == 0)
            return;

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
                    Logger.Error($"[重传-{_channel}] ProcessType: {source.ProcessType} 反序列化失败，删除记录");
                    await _failedStore.DeleteAsync(source.Id).ConfigureAwait(false);
                    continue;
                }

                completedRecords.Add(new CellCompletedRecord { CellData = cellData });
                validSourceRecords.Add(source);
            }

            if (completedRecords.Count == 0)
                continue;

            var success = await _cloudBatchConsumer!.ProcessBatchAsync(completedRecords).ConfigureAwait(false);
            if (success)
            {
                foreach (var source in validSourceRecords)
                    await _failedStore.DeleteAsync(source.Id).ConfigureAwait(false);

                Logger.Info($"[重传-{_channel}] Injection 批量补传成功，条数={validSourceRecords.Count}");
                continue;
            }

            foreach (var source in validSourceRecords)
            {
                await HandleRetryFailureAsync(source, source.FailedTarget, "云端批量补传失败")
                    .ConfigureAwait(false);
            }

            Logger.Warn($"[重传-{_channel}] Injection 批量补传失败，条数={validSourceRecords.Count}");
            break;
        }

        foreach (var record in others)
            await ProcessOneAsync(record).ConfigureAwait(false);
    }

    private async Task ProcessOneAsync(FailedCellRecord record)
    {
        var startIndex = _consumers.FindIndex(c => c.Name == record.FailedTarget);
        if (startIndex < 0)
        {
            Logger.Warn($"[重传-{_channel}] 消费者 {record.FailedTarget} 不存在，删除记录");
            await _failedStore.DeleteAsync(record.Id);
            return;
        }

        var cellData = DeserializeCellData(record.ProcessType, record.CellDataJson);
        if (cellData is null)
        {
            Logger.Error($"[重传-{_channel}] ProcessType: {record.ProcessType} 反序列化失败，删除记录");
            await _failedStore.DeleteAsync(record.Id);
            return;
        }

        var completedRecord = new CellCompletedRecord { CellData = cellData };
        var label = cellData.DisplayLabel;

        for (int i = startIndex; i < _consumers.Count; i++)
        {
            var consumer = _consumers[i];

            if (consumer.RetryChannel != _channel)
                continue;

            try
            {
                var success = await consumer.ProcessAsync(completedRecord).ConfigureAwait(false);

                if (!success)
                {
                    Logger.Warn($"[重传-{_channel}] {label}，{consumer.Name} 仍然失败");
                    await HandleRetryFailureAsync(record, consumer.Name, "消费者返回失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[重传-{_channel}] {label}，{consumer.Name} 异常: {ex.Message}");
                await HandleRetryFailureAsync(record, consumer.Name, ex.Message);
                return;
            }
        }

        await _failedStore.DeleteAsync(record.Id);
        Logger.Info($"[重传-{_channel}] {label} 补传成功，已从重传队列移除");
    }

    private async Task HandleRetryFailureAsync(
        FailedCellRecord record,
        string failedTarget,
        string errorMessage)
    {
        var newRetryCount = record.RetryCount + 1;

        if (newRetryCount > MaxRetryCount)
        {
            Logger.Warn($"[重传-{_channel}] {record.ProcessType} " +
                $"已达最大重试次数 {MaxRetryCount}，停止自动重传");
            await _failedStore.UpdateRetryAsync(
                record.Id, newRetryCount, errorMessage, DateTime.MaxValue);
            return;
        }

        var nextRetryTime = DateTime.Now.Add(CalculateBackoff(newRetryCount));
        await _failedStore.UpdateRetryAsync(
            record.Id, newRetryCount, errorMessage, nextRetryTime);
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
            return processType switch
            {
                "Injection" => JsonSerializer.Deserialize<InjectionCellData>(json, _jsonOptions),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"[重传-{_channel}] CellData 反序列化失败: {ex.Message}");
            return null;
        }
    }
}
