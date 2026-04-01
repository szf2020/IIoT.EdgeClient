namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// 通用定时同步任务接口
/// 
/// 两个职责：
///   1. StartAsync/StopAsync — 定时实时上传（各 SyncTask 自己跑）
///   2. RetryBufferAsync — 补传 SQLite 缓冲（由 RetryTask 统一调度）
/// </summary>
public interface ISyncTask
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();

    /// <summary>
    /// 补传 SQLite 离线缓冲，由 RetryTask 调用
    /// </summary>
    /// <returns>本轮是否全部补传成功</returns>
    Task<bool> RetryBufferAsync();
}