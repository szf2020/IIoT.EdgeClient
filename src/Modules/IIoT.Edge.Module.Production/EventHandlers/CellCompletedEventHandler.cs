using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Events;
using MediatR;

namespace IIoT.Edge.Module.Production.EventHandlers;

/// <summary>
/// 电芯完成事件处理器（UI 层）
/// 收到 CellCompletedEvent 后刷新生产监控界面
/// </summary>
public class CellCompletedEventHandler : INotificationHandler<CellCompletedEvent>
{
    private readonly ILogService _logger;

    public CellCompletedEventHandler(ILogService logger)
    {
        _logger = logger;
    }

    public Task Handle(CellCompletedEvent notification, CancellationToken cancellationToken)
    {
        var cellData = notification.Record.CellData;

        var result = cellData.CellResult switch
        {
            true => "OK",
            false => "NG",
            _ => "未判定"
        };

        _logger.Info($"[UI事件] 收到电芯完成通知 — {cellData.DisplayLabel}" +
            $"，工序: {cellData.ProcessType}" +
            $"，结果: {result}");

        // TODO: 将来在这里刷新各个 Widget

        return Task.CompletedTask;
    }
}