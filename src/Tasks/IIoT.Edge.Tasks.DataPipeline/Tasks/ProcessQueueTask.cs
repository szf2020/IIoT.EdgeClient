using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Tasks.Base;
using IIoT.Edge.Tasks.DataPipeline.Services;

namespace IIoT.Edge.Tasks.DataPipeline.Tasks;

/// <summary>
/// 主队列消费任务
/// 
/// 从内存队列中取出电芯数据，按消费者注册顺序严格串行执行：
///   Cloud上报 → Excel写入 → UI通知
/// 
/// 任何一个消费者失败 → 存入 SQLite 重传队列
/// 全局任务，不绑定特定设备
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
                    Logger.Warn($"[{record.CellData.ProcessType}] {consumer.Name} 处理失败" +
                        $"，{label}，转入重传队列");

                    await SaveToRetryAsync(record, consumer.Name, "消费者返回失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[{record.CellData.ProcessType}] {consumer.Name} 处理异常" +
                    $"，{label}，{ex.Message}");

                await SaveToRetryAsync(record, consumer.Name, ex.Message);
                return;
            }
        }

        Logger.Info($"[{record.CellData.ProcessType}] {label} 全部消费完成");
    }

    private async Task SaveToRetryAsync(
        CellCompletedRecord record,
        string failedTarget,
        string errorMessage)
    {
        try
        {
            await _failedStore.SaveAsync(record, failedTarget, errorMessage);
        }
        catch (Exception ex)
        {
            Logger.Error($"[{record.CellData.ProcessType}] 写入重传队列失败！" +
                $"{record.CellData.DisplayLabel}，{ex.Message}");
        }
    }
}