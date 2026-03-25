using IIoT.Edge.Common.DataPipeline;

namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// 电芯数据消费者接口
/// 
/// 每个消费者负责一个环节的数据处理：
///   MES上报、云端上报、Excel写入、UI通知等
/// 
/// ProcessQueueTask 按 Order 顺序严格串行调用
/// 任何一个返回 false，该条记录立即进入重传队列
/// </summary>
public interface ICellDataConsumer
{
    /// <summary>
    /// 消费者名称（用于日志和失败记录的 FailedTarget 字段）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 优先级（数值越小越先执行）
    /// MES=10, Cloud=20, Excel=30, UI=40
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 处理一条电芯数据
    /// </summary>
    Task<bool> ProcessAsync(CellCompletedRecord record);
}