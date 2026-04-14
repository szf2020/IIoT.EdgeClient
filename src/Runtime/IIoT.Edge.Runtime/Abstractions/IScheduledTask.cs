using IIoT.Edge.Application.Abstractions.Tasks;

namespace IIoT.Edge.Runtime.Abstractions;

/// <summary>
/// 后台定时类任务标记接口。
/// </summary>
public interface IScheduledTask : IBackgroundTask
{
    /// <summary>
    /// 执行间隔（毫秒）。
    /// </summary>
    int Interval { get; }

    /// <summary>
    /// 累计执行次数。
    /// </summary>
    long ExecutionCount { get; }

    /// <summary>
    /// 累计失败次数。
    /// </summary>
    long FailureCount { get; }

    /// <summary>
    /// 最近一次执行时间。
    /// </summary>
    DateTime? LastExecutedTime { get; }
}
