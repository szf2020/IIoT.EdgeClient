using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Events;
using MediatR;

namespace IIoT.Edge.Module.Production.EventHandlers;

/// <summary>
/// 电芯完成事件处理器（UI 层）
/// 
/// 收到 CellCompletedEvent 后刷新生产监控界面
/// 
/// TODO: 后续补充真实的 UI 刷新逻辑
///   - 更新 MonitorWidget 的电芯数据
///   - 更新 DataViewWidget 的产能统计
///   - 更新 CapacityViewWidget 的产能图表
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
        var record = notification.Record;

        _logger.Info($"[UI事件] 收到电芯完成通知 — 条码: {record.Barcode}" +
            $"，设备: {record.DeviceName}" +
            $"，结果: {(record.CellResult ? "OK" : "NG")}");

        // TODO: 将来在这里刷新各个 Widget
        // _monitorWidget.RefreshCellData(record);
        // _dataViewWidget.RefreshTodayStats();

        return Task.CompletedTask;
    }
}