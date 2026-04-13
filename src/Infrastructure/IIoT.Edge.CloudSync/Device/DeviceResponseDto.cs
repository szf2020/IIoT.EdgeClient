namespace IIoT.Edge.CloudSync.Device;

/// <summary>
/// 云端寻址接口 GET /api/v1/Device/instance 的返回 DTO
/// 
/// 只取云端需要的字段，不含 DeviceCode（已废弃）
/// </summary>
internal sealed class DeviceResponseDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Guid ProcessId { get; set; }
}
