using IIoT.Edge.Common.DataPipeline;

namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// 数据管道入队服务
/// 
/// 组装确认 Task 自己从 Context 取 CellBag 打包成 CellCompletedRecord
/// 然后调 Enqueue 推入内存队列，之后即可 RemoveCell
/// </summary>
public interface IDataPipelineService
{
    /// <summary>
    /// 将打包好的电芯完成记录推入消费队列
    /// </summary>
    void Enqueue(CellCompletedRecord record);

    /// <summary>
    /// 当前内存队列中待处理的数量（UI监控用）
    /// </summary>
    int PendingCount { get; }
}