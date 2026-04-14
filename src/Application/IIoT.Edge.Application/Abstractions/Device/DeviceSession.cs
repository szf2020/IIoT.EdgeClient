namespace IIoT.Edge.Application.Abstractions.Device;

/// <summary>
/// 设备会话信息。
///
/// 在心跳寻址成功后生成，表示当前客户端已识别到对应的云端设备实例。
/// DeviceId 用于数据上报，DeviceName 用于界面展示，MacAddress 保留为设备实例标识。
/// </summary>
public record DeviceSession
{
    /// <summary>
    /// 云端分配的设备 ID，用于数据上报时标识设备实例。
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// 设备名称，主要用于界面展示。
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// 设备实例标识，用于云端寻址。
    /// 字段名沿用 MacAddress，以兼容现有代码。
    /// </summary>
    public string MacAddress { get; init; } = string.Empty;

    /// <summary>
    /// 所属工序 ID。
    /// </summary>
    public Guid ProcessId { get; init; }
}
