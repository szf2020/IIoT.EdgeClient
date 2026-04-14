namespace IIoT.Edge.Infrastructure.Integration.Device;

/// <summary>
/// 云端寻址接口 `GET /api/v1/Device/instance` 返回的传输对象。
/// 
/// 仅保留客户端实际需要的字段，不包含已废弃的 DeviceCode。
/// </summary>
internal sealed class DeviceResponseDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Guid ProcessId { get; set; }
}
