using IIoT.Edge.Common.DataPipeline;

namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// 电芯数据消费者接口
/// 
/// ProcessQueueTask 按 Order 顺序依次调用每个消费者
/// 任何一步失败不阻塞后续消费者
/// 
/// RetryChannel 决定失败后的补传策略：
///   null      → 失败不入重传队列（纯本地操作，如 Excel、UI）
///   "Cloud"   → 进云端补传队列（CloudRetryTask 负责）
///   "MES"     → 进 MES 补传队列（MesRetryTask 负责）
/// </summary>
public interface ICellDataConsumer
{
    /// <summary>
    /// 消费者标识（日志 / 重传定位用）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 执行顺序，数字越小越先执行
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 失败后进哪个补传通道
    /// null = 不补传（纯本地操作）
    /// "Cloud" / "MES" = 按通道补传
    /// </summary>
    string? RetryChannel { get; }

    /// <summary>
    /// 处理一条电芯完成记录
    /// true = 成功，false = 失败（按 RetryChannel 决定是否入重传队列）
    /// </summary>
    Task<bool> ProcessAsync(CellCompletedRecord record);
}