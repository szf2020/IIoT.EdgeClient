namespace IIoT.Edge.Contracts.Device;

/// <summary>
/// 设备会话信息
/// 
/// 心跳寻址成功后生成，云端实例身份是 MAC + ClientCode
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
    /// 设备实例标识（用于云端寻址，字段名沿用 MacAddress 兼容既有代码）
    /// </summary>
    public string MacAddress { get; init; } = string.Empty;

    /// <summary>
    /// 所属工序 ID
    /// </summary>
    public Guid ProcessId { get; init; }
}
