namespace IIoT.Edge.Application.Abstractions.Device;

/// <summary>
/// 设备服务接口。
///
/// 职责：
/// 1. 在运行时识别当前设备并维护设备会话。
/// 2. 周期性执行心跳，更新网络状态。
/// 3. 在网络状态或设备会话变化时向外广播。
///
/// 心跳策略：
/// Online：每 1 分钟检查一次，确认会话是否仍然有效。
/// Offline：每 10 秒检查一次，尽快恢复。
///
/// 会话状态说明：
/// - CurrentDevice 为 null：尚未完成设备识别。
/// - CurrentDevice 不为 null 且离线：设备已识别，但当前网络不可用。
/// - CurrentDevice 不为 null 且在线：设备已识别，可执行实时上报。
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// 当前设备会话；设备识别成功前为 null。
    /// </summary>
    DeviceSession? CurrentDevice { get; }

    /// <summary>
    /// 当前网络状态，默认值为 Offline。
    /// </summary>
    NetworkState CurrentState { get; }

    /// <summary>
    /// 是否已获取到有效的 DeviceId。
    /// </summary>
    bool HasDeviceId { get; }

    /// <summary>
    /// 启动心跳服务。
    /// </summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>
    /// 停止心跳服务。
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 网络状态变化时触发，例如切换到 Online 或 Offline。
    /// </summary>
    event Action<NetworkState> NetworkStateChanged;

    /// <summary>
    /// 设备会话变化时触发，例如识别成功或会话信息更新。
    /// </summary>
    event Action<DeviceSession?> DeviceIdentified;
}
