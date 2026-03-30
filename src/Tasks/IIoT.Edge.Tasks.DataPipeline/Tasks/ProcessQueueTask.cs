using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Tasks.Base;
using IIoT.Edge.Tasks.DataPipeline.Services;

namespace IIoT.Edge.Tasks.DataPipeline.Tasks;

/// <summary>
/// 主队列消费任务
/// 
/// 从内存队列中取出电芯数据，按消费者 Order 严格串行执行：
///   Capacity → MES → Cloud → Excel → UI
/// 
/// 核心原则：任何一个消费者失败 **不阻塞后续消费者**
///   - RetryChannel != null 的消费者失败 → 按 channel 存入重传队列
///   - RetryChannel == null 的消费者失败 → 仅记日志，不入队列
///   - 无论成功失败，继续执行下一个消费者
/// </summary>
public class ProcessQueueTask : ScheduledTaskBase
{
    private readonly DataPipelineService _pipelineService;
    private readonly List<ICellDataConsumer> _consumers;
    private readonly IFailedRecordStore _failedStore;

    public override string TaskName => "ProcessQueueTask";

    protected override int ExecuteInterval => 50;

    public ProcessQueueTask(
        ILogService logger,
        DataPipelineService pipelineService,
        IEnumerable<ICellDataConsumer> consumers,
        IFailedRecordStore failedStore)
        : base(logger)
    {
        _pipelineService = pipelineService;
        _failedStore = failedStore;
        _consumers = consumers.OrderBy(c => c.Order).ToList();
    }

    protected override async Task ExecuteAsync()
    {
        if (!_pipelineService.Queue.TryDequeue(out var record))
            return;

        var label = record.CellData.DisplayLabel;
        Logger.Info($"[{record.CellData.ProcessType}] 开始处理: {label}");

        foreach (var consumer in _consumers)
        {
            try
            {
                var success = await consumer.ProcessAsync(record).ConfigureAwait(false);

                if (!success)
                {
                    await HandleFailureAsync(record, consumer, "消费者返回失败");
                    // 不 return，继续下一个消费者
                }
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(record, consumer, ex.Message);
                // 不 return，继续下一个消费者
            }
        }

        Logger.Info($"[{record.CellData.ProcessType}] {label} 消费链执行完毕");
    }

    /// <summary>
    /// 处理消费者失败
    /// RetryChannel 不为 null → 按通道存入重传队列
    /// RetryChannel 为 null   → 仅记日志
    /// </summary>
    private async Task HandleFailureAsync(
        CellCompletedRecord record,
        ICellDataConsumer consumer,
        string errorMessage)
    {
        var label = record.CellData.DisplayLabel;

        if (consumer.RetryChannel is null)
        {
            // 纯本地消费者（Capacity/Excel/UI），失败不补传
            Logger.Warn($"[{record.CellData.ProcessType}] {consumer.Name} 失败" +
                $"，{label}，{errorMessage}（不补传）");
            return;
        }

        // 有补传通道的消费者（Cloud/MES），存入重传队列
        Logger.Warn($"[{record.CellData.ProcessType}] {consumer.Name} 失败" +
            $"，{label}，转入 {consumer.RetryChannel} 重传队列");

        try
        {
            await _failedStore.SaveAsync(
                record, consumer.Name, errorMessage, consumer.RetryChannel);
        }
        catch (Exception ex)
        {
            Logger.Error($"[{record.CellData.ProcessType}] 写入重传队列失败！" +
                $"{label}，{ex.Message}");
        }
    }
}