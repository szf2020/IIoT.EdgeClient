namespace IIoT.Edge.Contracts.Device
{
    public interface IDeviceService
    {
        /// <summary>当前设备会话，寻址成功前为 null</summary>
        DeviceSession? CurrentDevice { get; }

        /// <summary>是否已完成寻址</summary>
        bool IsIdentified { get; }

        /// <summary>启动时自动读取MAC并向云端寻址</summary>
        Task<bool> IdentifyAsync();

        /// <summary>寻址完成后触发</summary>
        event Action<DeviceSession?> DeviceIdentified;
    }
}