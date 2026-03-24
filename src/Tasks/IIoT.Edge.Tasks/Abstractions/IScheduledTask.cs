// 路径：src/Infrastructure/IIoT.Edge.Tasks/Abstractions/IScheduledTask.cs
using IIoT.Edge.Contracts.Plc;

namespace IIoT.Edge.Tasks.Abstractions;

/// <summary>
/// 后台定时类任务的标记接口
/// 适用于所有周期执行、不跟PLC握手的任务
/// 
/// 如：过站队列消费、补传重发、定时数据上传
/// </summary>
public interface IScheduledTask : IPlcTask
{
    /// <summary>
    /// 执行间隔（ms）
    /// </summary>
    int Interval { get; }

    /// <summary>
    /// 累计执行次数
    /// </summary>
    long ExecutionCount { get; }

    /// <summary>
    /// 累计失败次数
    /// </summary>
    long FailureCount { get; }

    /// <summary>
    /// 最近一次执行时间
    /// </summary>
    DateTime? LastExecutedTime { get; }
}