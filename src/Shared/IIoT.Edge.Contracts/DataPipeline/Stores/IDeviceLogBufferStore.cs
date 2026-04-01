using IIoT.Edge.Common.DataPipeline.DeviceLog;

namespace IIoT.Edge.Contracts.DataPipeline.Stores;

/// <summary>
/// 设备日志离线缓冲接口
/// 
/// 写入方：DeviceLogSyncTask（POST 失败或离线时）
/// 读取方：RetryTask[Cloud]（分批捞出补传）
/// </summary>
public interface IDeviceLogBufferStore
{
    /// <summary>
    /// 批量写入缓冲
    /// </summary>
    Task SaveBatchAsync(IEnumerable<DeviceLogRecord> records);

    /// <summary>
    /// 捞一批待补传的记录
    /// </summary>
    Task<List<DeviceLogRecord>> GetPendingAsync(int batchSize = 100);

    /// <summary>
    /// 补传成功后按 Id 批量删除
    /// </summary>
    Task DeleteBatchAsync(IEnumerable<long> ids);

    /// <summary>
    /// 缓冲区记录数（诊断用）
    /// </summary>
    Task<int> GetCountAsync();
}