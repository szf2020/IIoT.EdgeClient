using IIoT.Edge.Common.DataPipeline;

namespace IIoT.Edge.Contracts.DataPipeline.Stores;

/// <summary>
/// 失败记录存储接口
/// 
/// ProcessQueueTask 消费失败时调用 SaveAsync 存入 SQLite
/// RetryTask 按 channel 定时调用 GetPendingAsync 捞出来重试
/// 重试成功后调用 DeleteAsync 删除记录
/// </summary>
public interface IFailedRecordStore
{
    /// <summary>
    /// 存入一条失败记录
    /// </summary>
    /// <param name="channel">补传通道："Cloud" / "MES"</param>
    Task SaveAsync(CellCompletedRecord record, string failedTarget,
                   string errorMessage, string channel);

    /// <summary>
    /// 获取待重试的记录（按通道过滤，NextRetryTime 已到期的）
    /// </summary>
    Task<List<FailedCellRecord>> GetPendingAsync(string channel, int batchSize = 10);

    /// <summary>
    /// 重试成功，删除记录
    /// </summary>
    Task DeleteAsync(long id);

    /// <summary>
    /// 重试失败，更新重试次数和下次重试时间
    /// </summary>
    Task UpdateRetryAsync(long id, int retryCount, string errorMessage, DateTime nextRetryTime);

    /// <summary>
    /// 获取当前失败记录总数（UI监控用）
    /// </summary>
    Task<int> GetCountAsync();

    /// <summary>
    /// 按通道获取失败记录总数
    /// </summary>
    Task<int> GetCountAsync(string channel);

    /// <summary>
    /// 重置所有 Abandoned 记录为可重试（UI "全部重传" 按钮用）
    /// </summary>
    Task ResetAllAbandonedAsync();
}