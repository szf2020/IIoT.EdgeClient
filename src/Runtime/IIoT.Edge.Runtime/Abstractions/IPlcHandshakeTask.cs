using IIoT.Edge.Application.Abstractions.Plc;

namespace IIoT.Edge.Runtime.Abstractions;

/// <summary>
/// PLC 握手类任务标记接口。
/// </summary>
public interface IPlcHandshakeTask : IPlcTask
{
    /// <summary>
    /// 当前状态机步骤。
    /// </summary>
    int CurrentStep { get; }

    /// <summary>
    /// 最近一次执行是否成功。
    /// </summary>
    bool? LastResult { get; }

    /// <summary>
    /// 最近一次完成时间。
    /// </summary>
    DateTime? LastCompletedTime { get; }
}
