using IIoT.Edge.Common.Context;
using IIoT.Edge.Contracts.Plc.Store;

namespace IIoT.Edge.Contracts.Plc;

/// <summary>
/// PLC 连接管理器契约
///
/// 负责：PLC 连接建立、Task 调度、Buffer 初始化、设备热重载
/// 实现在 Infrastructure 层（IIoT.Edge.PlcDevice）
/// Shell / AppLifecycleManager 只依赖此接口，不引用任何具体实现
/// </summary>
public interface IPlcConnectionManager : IDisposable
{
    /// <summary>
    /// 启动时初始化所有已启用的 PLC 设备
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// 配置保存后热重载指定设备（不影响其他设备上下文）
    /// </summary>
    Task ReloadAsync(string deviceName, CancellationToken ct = default);

    /// <summary>
    /// 按设备名注册任务工厂（机台专属模块在启动时调用）
    /// </summary>
    void RegisterTasks(
        string deviceName,
        Func<IPlcBuffer, ProductionContext, List<IPlcTask>> factory);

    /// <summary>
    /// 获取指定设备的 PLC 通信实例
    /// </summary>
    IPlcService? GetPlc(int networkDeviceId);

    /// <summary>
    /// 获取指定设备的生产上下文
    /// </summary>
    ProductionContext? GetContext(string deviceName);
}
