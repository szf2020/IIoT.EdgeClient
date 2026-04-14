namespace IIoT.Edge.Application.Abstractions.Plc;

/// <summary>
/// 信号交互任务契约。
/// 在 PLC 任务基础上补充连接状态与连接建立能力。
/// </summary>
public interface ISignalInteraction : IPlcTask
{
    bool IsConnected { get; }
    Task ConnectAsync();
}
