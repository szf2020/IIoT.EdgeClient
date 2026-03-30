using IIoT.Edge.Common.DataPipeline.Capacity;

namespace IIoT.Edge.Contracts.DataPipeline.Stores;

/// <summary>
/// 当天产能内存存储接口
/// 
/// 实现方维护 TodayCapacity 内存对象，跟随 ProductionContext 持久化
/// 
/// 写入方：CapacityConsumer（每个电芯完成时 Increment）
/// 读取方：CapacitySyncTask（30分钟读快照POST云端）、UI（实时显示）
/// </summary>
public interface ITodayCapacityStore
{
    /// <summary>
    /// 产能计数 +1，内部自动判定班次、跨天清零
    /// </summary>
    /// <returns>本次归属的班次编码："D"=白班, "N"=夜班</returns>
    string Increment(DateTime completedTime, bool isOk);

    /// <summary>
    /// 获取当天产能快照（UI 绑定 / 云端上传用）
    /// </summary>
    TodayCapacity GetSnapshot();

    /// <summary>
    /// 手动清零（换班 / 调试用）
    /// </summary>
    void Reset();
}