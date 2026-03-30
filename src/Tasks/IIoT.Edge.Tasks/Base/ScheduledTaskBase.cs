// 路径：src/Infrastructure/IIoT.Edge.Tasks/Base/ScheduledTaskBase.cs
using IIoT.Edge.Common.Context;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Tasks.Context;

namespace IIoT.Edge.Tasks.Base;

/// <summary>
/// 后台定时类任务基类
/// 适用于：不跟PLC握手，按固定间隔周期执行的任务
/// 如：定时数据上传、过站队列消费、补传重发
/// 
/// 与 PlcTaskBase 的区别：
/// 1. 不依赖 IPlcBuffer（不读写PLC缓冲区）
/// 2. 执行间隔通常更长（秒~分钟级，而非10ms级）
/// 3. 没有状态机步骤（或由子类自行管理）
/// </summary>
public abstract class ScheduledTaskBase : IPlcTask
{
    protected readonly ProductionContext? Context;
    protected readonly ILogService Logger;

    /// <summary>
    /// 任务唯一标识
    /// </summary>
    public abstract string TaskName { get; }

    /// <summary>
    /// 执行间隔（ms），子类必须指定
    /// 如：队列消费 50ms，补传 1小时，定时上传 10s
    /// </summary>
    protected abstract int ExecuteInterval { get; }

    /// <summary>
    /// 异常后等待间隔（ms）
    /// </summary>
    protected virtual int ErrorRetryInterval => 1000;

    /// <summary>
    /// 带 Context 的构造（绑定某台设备的定时任务，如定时上传该设备数据）
    /// </summary>
    protected ScheduledTaskBase(ProductionContext context, ILogService logger)
    {
        Context = context;
        Logger = logger;
    }

    /// <summary>
    /// 不带 Context 的构造（全局定时任务，如补传、队列消费，不绑定特定设备）
    /// </summary>
    protected ScheduledTaskBase(ILogService logger)
    {
        Context = null;
        Logger = logger;
    }

    /// <summary>
    /// 子类实现：一轮执行逻辑
    /// </summary>
    protected abstract Task ExecuteAsync();

    public async Task StartAsync(CancellationToken ct)
    {
        await Task.Factory.StartNew(
            () => TaskCoreAsync(ct),
            ct,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    private async Task TaskCoreAsync(CancellationToken ct)
    {
        var deviceInfo = Context is not null
            ? $"[{Context.DeviceName}] "
            : "";

        Logger.Info($"{deviceInfo}{TaskName} 启动，执行间隔: {ExecuteInterval}ms");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ExecuteAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"{deviceInfo}{TaskName} 异常: {ex.Message}");
                await Task.Delay(ErrorRetryInterval, ct);
            }

            await Task.Delay(ExecuteInterval, ct);
        }

        Logger.Info($"{deviceInfo}{TaskName} 已停止");
    }
}