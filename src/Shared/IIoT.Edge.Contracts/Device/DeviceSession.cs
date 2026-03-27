namespace IIoT.Edge.Contracts.Device;

/// <summary>
/// 设备会话信息
/// 
/// 心跳寻址成功后生成，MAC 地址是唯一标识
/// DeviceId 是云端分配的内部 ID，用于数据上报
/// DeviceName 纯显示用
/// 
/// CurrentDevice 为 null 表示从未寻址成功过
/// CurrentDevice 不为 null 但 Offline 表示有缓存但网络不通
/// </summary>
public record DeviceSession
{
    /// <summary>
    /// 云端分配的设备 ID（UUID），数据上报时携带
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// 设备名称（显示用，来自云端）
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// 本机 MAC 地址（唯一标识）
    /// </summary>
    public string MacAddress { get; init; } = string.Empty;

    /// <summary>
    /// 所属工序 ID
    /// </summary>
    public Guid ProcessId { get; init; }
}