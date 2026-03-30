using IIoT.Edge.Common.DataPipeline.Capacity;

namespace IIoT.Edge.Contracts.DataPipeline.Stores;

/// <summary>
/// 当天产能内存存储接口
/// 
/// 通过 deviceName 定位对应 ProductionContext 的 TodayCapacity
/// 
/// 写入方：CapacityConsumer（每个电芯完成时 Increment）
/// 读取方：CapacitySyncTask（30分钟读快照POST云端）、UI（实时显示）
/// </summary>
public interface ITodayCapacityStore
{
    /// <summary>
    /// 产能计数 +1，内部自动判定班次、跨天清零
    /// </summary>
    /// <param name="deviceName">设备名（聚合 key，如"注液机1"）</param>
    /// <returns>本次归属的班次编码："D"=白班, "N"=夜班</returns>
    string Increment(string deviceName, DateTime completedTime, bool isOk);

    /// <summary>
    /// 获取指定设备的当天产能快照
    /// </summary>
    TodayCapacity GetSnapshot(string deviceName);

    /// <summary>
    /// 手动清零指定设备
    /// </summary>
    void Reset(string deviceName);
}