using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using System.Collections.Concurrent;

namespace IIoT.Edge.Tasks.DataPipeline.Services;

/// <summary>
/// 数据管道入队服务实现
/// 
/// 职责：
///   1. 接收组装确认 Task 打包好的 CellCompletedRecord（含强类型 CellData）
///   2. 放入 ConcurrentQueue
///   3. 由 ProcessQueueTask 负责消费
/// </summary>
public class DataPipelineService : IDataPipelineService
{
    private readonly ConcurrentQueue<CellCompletedRecord> _queue = new();
    private readonly ILogService _logger;

    public DataPipelineService(ILogService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 当前队列待处理数量
    /// </summary>
    public int PendingCount => _queue.Count;

    /// <summary>
    /// 内部队列引用（仅供 ProcessQueueTask 消费用）
    /// </summary>
    internal ConcurrentQueue<CellCompletedRecord> Queue => _queue;

    public void Enqueue(CellCompletedRecord record)
    {
        if (record is null)
        {
            _logger.Warn("[DataPipeline] 入队失败：record 为 null");
            return;
        }

        if (record.CellData is null)
        {
            _logger.Warn("[DataPipeline] 入队失败：CellData 为 null");
            return;
        }

        _queue.Enqueue(record);

        var cellData = record.CellData;
        var result = cellData.CellResult switch
        {
            true => "OK",
            false => "NG",
            null => "未判定"
        };

        _logger.Info($"[{cellData.DeviceCode}] {cellData.ProcessType} 已入队" +
            $"（结果:{result}，队列积压:{_queue.Count}）");
    }
}