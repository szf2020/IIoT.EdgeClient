
using IIoT.Edge.Common.Context;

namespace IIoT.Edge.Contracts.Context;

/// <summary>
/// 生产上下文管理接口
/// 
/// 按 DeviceName 管理所有 PLC 设备的 ProductionContext
/// DeviceName 是全局唯一聚合 key
/// 
/// 实现方负责：内存管理 + JSON 持久化
/// </summary>
public interface IProductionContextStore
{
    /// <summary>
    /// 获取指定设备的 Context，不存在则创建
    /// </summary>
    ProductionContext GetOrCreate(string deviceName);

    /// <summary>
    /// 获取所有 Context（UI 展示 / 遍历上传用）
    /// </summary>
    IReadOnlyCollection<ProductionContext> GetAll();

    /// <summary>
    /// 从本地文件恢复（启动时调用）
    /// </summary>
    void LoadFromFile();

    /// <summary>
    /// 保存到本地文件（退出时 / 定时调用）
    /// </summary>
    void SaveToFile();

    /// <summary>
    /// 定时自动保存
    /// </summary>
    Task StartAutoSaveAsync(CancellationToken ct, int intervalSeconds = 30);
}