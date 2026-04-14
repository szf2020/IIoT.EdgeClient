using IIoT.Edge.SharedKernel.DataPipeline.DeviceLog;

namespace IIoT.Edge.Application.Abstractions.DataPipeline.Stores;

/// <summary>
/// 设备日志离线缓冲接口。
/// 
/// 写入方：DeviceLogSyncTask，在提交失败或设备离线时写入。
/// 读取方：Cloud 通道的 RetryTask，按批读取并补传。
/// </summary>
public interface IDeviceLogBufferStore
{
    /// <summary>
    /// 批量写入缓冲记录。
    /// </summary>
    Task SaveBatchAsync(IEnumerable<DeviceLogRecord> records);

    /// <summary>
    /// 获取一批待补传记录。
    /// </summary>
    Task<List<DeviceLogRecord>> GetPendingAsync(int batchSize = 100);

    /// <summary>
    /// 补传成功后按 Id 批量删除记录。
    /// </summary>
    Task DeleteBatchAsync(IEnumerable<long> ids);

    /// <summary>
    /// 获取缓冲区记录数，供诊断使用。
    /// </summary>
    Task<int> GetCountAsync();
}
