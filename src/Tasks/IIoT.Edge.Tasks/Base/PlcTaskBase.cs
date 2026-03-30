// 路径：src/Infrastructure/IIoT.Edge.Tasks/Base/PlcTaskBase.cs
using IIoT.Edge.Common.Context;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Tasks.Context;

namespace IIoT.Edge.Tasks.Base;

/// <summary>
/// PLC握手类任务基类
/// 适用于：PLC触发 → 执行业务 → 写回结果 → 等PLC确认 的状态机模式
/// 子类只需实现 DoCoreAsync，状态步骤通过 ProductionContext 持久化
/// </summary>
public abstract class PlcTaskBase : IPlcTask
{
    protected readonly IPlcBuffer Buffer;
    protected readonly ProductionContext Context;
    protected readonly ILogService Logger;

    /// <summary>
    /// 任务唯一标识（同时作为 StepStates 的 key）
    /// </summary>
    public abstract string TaskName { get; }

    /// <summary>
    /// 正常轮询间隔（ms），子类可覆盖
    /// </summary>
    protected virtual int TaskLoopInterval => 10;

    /// <summary>
    /// 异常后等待间隔（ms），子类可覆盖
    /// </summary>
    protected virtual int ErrorRetryInterval => 1000;

    /// <summary>
    /// 当前状态机步骤（读写自动走 ProductionContext，支持持久化恢复）
    /// </summary>
    protected int Step
    {
        get => Context.GetStep(TaskName);
        set => Context.SetStep(TaskName, value);
    }

    protected PlcTaskBase(
        IPlcBuffer buffer,
        ProductionContext context,
        ILogService logger)
    {
        Buffer = buffer;
        Context = context;
        Logger = logger;
    }

    /// <summary>
    /// 子类实现：一轮状态机逻辑
    /// </summary>
    protected abstract Task DoCoreAsync();

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
        Logger.Info($"[{Context.DeviceName}] {TaskName} 启动，当前步骤: {Step}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await DoCoreAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"[{Context.DeviceName}] {TaskName} 异常: {ex.Message}");
                await Task.Delay(ErrorRetryInterval, ct);
            }

            await Task.Delay(TaskLoopInterval, ct);
        }

        Logger.Info($"[{Context.DeviceName}] {TaskName} 已停止");
    }
}