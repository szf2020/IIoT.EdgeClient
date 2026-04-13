namespace IIoT.Edge.Contracts.Device;

/// <summary>
/// 设备服务接口
/// 
/// 职责：
///   1. 读取稳定设备实例标识并携带 ClientCode 进行云端寻址
///   2. 心跳循环：定时调云端寻址接口探测网络状态
///   3. 成功 → Online，失败 → Offline
///   4. 状态切换时发布 NetworkStateChanged 事件
/// 
/// 心跳策略：
///   Online  → 1 分钟一次（确认还在线）
///   Offline → 10 秒一次（快速恢复）
/// 
/// 数据上报判断：
///   CurrentDevice == null        → 跳过上报（从未寻址成功）
///   CurrentDevice != null + Offline  → 存重传队列
///   CurrentDevice != null + Online   → 实时 POST 云端
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// 当前设备会话，从未寻址成功时为 null
    /// </summary>
    DeviceSession? CurrentDevice { get; }

    /// <summary>
    /// 当前网络状态，默认 Offline
    /// </summary>
    NetworkState CurrentState { get; }

    /// <summary>
    /// 是否有 DeviceId（寻址成功过，不管当前是否在线）
    /// </summary>
    bool HasDeviceId { get; }

    /// <summary>
    /// 启动心跳循环（App 启动时调用一次）
    /// 内部立即做一次寻址，然后按状态自适应间隔持续探测
    /// </summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>
    /// 停止心跳循环（App 退出时调用）
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 网络状态切换时触发（Online ↔ Offline）
    /// 订阅方：CloudConsumer、RetryTask、CapacitySyncTask 等
    /// </summary>
    event Action<NetworkState> NetworkStateChanged;

    /// <summary>
    /// 设备会话变更时触发（首次寻址成功 / 信息更新）
    /// 订阅方：UI 层更新 Footer 显示
    /// </summary>
    event Action<DeviceSession?> DeviceIdentified;
}
