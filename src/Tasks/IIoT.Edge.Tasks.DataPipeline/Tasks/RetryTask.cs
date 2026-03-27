using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Tasks.Base;
using System.Text.Json;

namespace IIoT.Edge.Tasks.DataPipeline.Tasks;

/// <summary>
/// 重传队列任务
/// 
/// 定时从 SQLite 捞出失败记录：
///   根据 ProcessType 反序列化 CellDataJson → 具体子类
///   包装成 CellCompletedRecord，从 FailedTarget 那步继续消费
/// 
/// 退避策略：
///   1-5次   → 30秒后重试
///   6-10次  → 5分钟后重试
///   11-20次 → 30分钟后重试
///   20次以上 → 停止自动重传
/// </summary>
public class RetryTask : ScheduledTaskBase
{
    private readonly IFailedRecordStore _failedStore;
    private readonly List<ICellDataConsumer> _consumers;

    private const int MaxRetryCount = 20;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public override string TaskName => "RetryTask";

    protected override int ExecuteInterval => 5000;

    public RetryTask(
        ILogService logger,
        IFailedRecordStore failedStore,
        IEnumerable<ICellDataConsumer> consumers)
        : base(logger)
    {
        _failedStore = failedStore;
        _consumers = consumers.OrderBy(c => c.Order).ToList();
    }

    protected override async Task ExecuteAsync()
    {
        var records = await _failedStore.GetPendingAsync(batchSize: 5).ConfigureAwait(false);

        if (records.Count == 0)
            return;

        foreach (var record in records)
        {
            await ProcessOneAsync(record).ConfigureAwait(false);
        }
    }

    private async Task ProcessOneAsync(FailedCellRecord record)
    {
        var startIndex = _consumers.FindIndex(c => c.Name == record.FailedTarget);
        if (startIndex < 0)
        {
            Logger.Warn($"[重传] ProcessType: {record.ProcessType}，消费者 {record.FailedTarget} 不存在，删除记录");
            await _failedStore.DeleteAsync(record.Id);
            return;
        }

        // 反序列化 CellDataJson → 强类型 CellData
        var cellData = DeserializeCellData(record.ProcessType, record.CellDataJson);
        if (cellData is null)
        {
            Logger.Error($"[重传] ProcessType: {record.ProcessType} 反序列化失败，删除记录");
            await _failedStore.DeleteAsync(record.Id);
            return;
        }

        var completedRecord = new CellCompletedRecord { CellData = cellData };
        var label = cellData.DisplayLabel;

        for (int i = startIndex; i < _consumers.Count; i++)
        {
            var consumer = _consumers[i];

            try
            {
                var success = await consumer.ProcessAsync(completedRecord).ConfigureAwait(false);

                if (!success)
                {
                    Logger.Warn($"[重传] {label}，{consumer.Name} 仍然失败");
                    await HandleRetryFailureAsync(record, consumer.Name, "消费者返回失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[重传] {label}，{consumer.Name} 异常: {ex.Message}");
                await HandleRetryFailureAsync(record, consumer.Name, ex.Message);
                return;
            }
        }

        await _failedStore.DeleteAsync(record.Id);
        Logger.Info($"[重传] {label} 补传成功，已从重传队列移除");
    }

    private async Task HandleRetryFailureAsync(
        FailedCellRecord record,
        string failedTarget,
        string errorMessage)
    {
        var newRetryCount = record.RetryCount + 1;

        if (newRetryCount > MaxRetryCount)
        {
            Logger.Warn($"[重传] ProcessType: {record.ProcessType} 已达最大重试次数 {MaxRetryCount}，停止自动重传");
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

    /// <summary>
    /// 根据 ProcessType 反序列化 CellDataJson → 具体子类
    /// 新增设备类型在 switch 里加一行 case
    /// </summary>
    private CellDataBase? DeserializeCellData(string processType, string json)
    {
        try
        {
            return processType switch
            {
                "Injection" => JsonSerializer.Deserialize<InjectionCellData>(json, _jsonOptions),
                // "DieCutting" => JsonSerializer.Deserialize<DieCuttingCellData>(json, _jsonOptions),
                // "PreCharge"  => JsonSerializer.Deserialize<PreChargeCellData>(json, _jsonOptions),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"[重传] CellData 反序列化失败: {ex.Message}");
            return null;
        }
    }
}