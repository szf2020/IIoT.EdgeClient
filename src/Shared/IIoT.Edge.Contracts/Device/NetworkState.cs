namespace IIoT.Edge.Contracts.Device;

/// <summary>
/// 设备网络状态枚举
/// 
/// 由 DeviceService 心跳循环维护
/// 心跳 = 定时调用云端寻址接口 GET /device/instance?macAddress=...&clientCode=...
/// 通了就是 Online，不通就是 Offline
/// </summary>
public enum NetworkState
{
    /// <summary>
    /// 网络不通，数据存本地（Excel + SQLite 重传队列）
    /// </summary>
    Offline,

    /// <summary>
    /// 网络正常，实时上报云端 + flush 积压数据
    /// </summary>
    Online
}
