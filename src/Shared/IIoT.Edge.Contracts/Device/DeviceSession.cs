// 路径：src/Shared/IIoT.Edge.Contracts/Device/DeviceSession.cs
namespace IIoT.Edge.Contracts.Device
{
    public record DeviceSession
    {
        public Guid DeviceId { get; init; }
        public string DeviceCode { get; init; } = string.Empty;
        public string DeviceName { get; init; } = string.Empty;
        public string MacAddress { get; init; } = string.Empty;
        public Guid ProcessId { get; init; }
    }
}