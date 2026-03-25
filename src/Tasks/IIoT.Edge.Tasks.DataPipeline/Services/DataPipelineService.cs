using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using System.Collections.Concurrent;

namespace IIoT.Edge.Tasks.DataPipeline.Services;

/// <summary>
/// 数据管道入队服务实现
/// 
/// 职责很简单：
///   1. 接收调用方打包好的 CellCompletedRecord
///   2. 放入 ConcurrentQueue
/// 
/// 序列化逻辑由调用方（组装确认 Task）负责
/// 数据已经在内存队列里，由 ProcessQueueTask 负责消费
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

        if (string.IsNullOrEmpty(record.Barcode))
        {
            _logger.Warn("[DataPipeline] 入队失败：Barcode 为空");
            return;
        }

        if (string.IsNullOrEmpty(record.DataJson))
        {
            _logger.Warn($"[{record.DeviceName}] 入队失败：条码 {record.Barcode} 的 DataJson 为空");
            return;
        }

        _queue.Enqueue(record);

        _logger.Info($"[{record.DeviceName}] 条码 {record.Barcode} 已入队" +
            $"（结果:{(record.CellResult ? "OK" : "NG")}，队列积压:{_queue.Count}）");
    }
}