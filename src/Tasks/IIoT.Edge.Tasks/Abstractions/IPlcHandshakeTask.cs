// 路径：src/Infrastructure/IIoT.Edge.Tasks/Abstractions/IPlcHandshakeTask.cs
using IIoT.Edge.Contracts.Plc;

namespace IIoT.Edge.Tasks.Abstractions;

/// <summary>
/// PLC握手类任务的标记接口
/// 适用于所有 PLC触发→执行→握手确认 模式的任务
/// 
/// 具体能力由各机台项目定义子接口，例如：
/// - IScanTask : IPlcHandshakeTask（扫码能力）
/// - IVoltageTestTask : IPlcHandshakeTask（电压检测能力）
/// 
/// 外部可通过此接口统一查询所有握手类任务的状态
/// </summary>
public interface IPlcHandshakeTask : IPlcTask
{
    /// <summary>
    /// 当前状态机步骤（0=空闲等待触发）
    /// </summary>
    int CurrentStep { get; }

    /// <summary>
    /// 最近一次执行是否成功（null=尚未执行过）
    /// </summary>
    bool? LastResult { get; }

    /// <summary>
    /// 最近一次执行完成的时间
    /// </summary>
    DateTime? LastCompletedTime { get; }
}