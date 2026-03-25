using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Tasks.Base;
using IIoT.Edge.Tasks.DataPipeline.Services;

namespace IIoT.Edge.Tasks.DataPipeline.Tasks;

/// <summary>
/// 主队列消费任务（对应老项目 ProcessQueueWorkTask）
/// 
/// 从内存队列中取出电芯数据，按消费者注册顺序严格串行执行：
///   MES上报 → 云端上报 → Excel写入 → UI通知 → 记产能
/// 
/// 任何一个消费者失败 → 记录失败信息 → 存入 SQLite 重传队列
/// 
/// 全局任务，不绑定特定设备（所有设备的电芯数据汇入同一个队列）
/// </summary>
public class ProcessQueueTask : ScheduledTaskBase
{
    private readonly DataPipelineService _pipelineService;
    private readonly List<ICellDataConsumer> _consumers;
    private readonly IFailedRecordStore _failedStore;

    public override string TaskName => "ProcessQueueTask";

    /// <summary>
    /// 队列消费间隔 50ms（与老项目一致）
    /// </summary>
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

        Logger.Info($"[{record.DeviceName}] 开始处理条码: {record.Barcode}");

        foreach (var consumer in _consumers)
        {
            try
            {
                var success = await consumer.ProcessAsync(record).ConfigureAwait(false);

                if (!success)
                {
                    Logger.Warn($"[{record.DeviceName}] {consumer.Name} 处理失败" +
                        $"，条码: {record.Barcode}，转入重传队列");

                    await SaveToRetryAsync(record, consumer.Name, "消费者返回失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[{record.DeviceName}] {consumer.Name} 处理异常" +
                    $"，条码: {record.Barcode}，{ex.Message}");

                await SaveToRetryAsync(record, consumer.Name, ex.Message);
                return;
            }
        }

        Logger.Info($"[{record.DeviceName}] 条码: {record.Barcode} 全部消费完成");
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
            Logger.Error($"[{record.DeviceName}] 写入重传队列失败！" +
                $"条码: {record.Barcode}，{ex.Message}");
        }
    }
}