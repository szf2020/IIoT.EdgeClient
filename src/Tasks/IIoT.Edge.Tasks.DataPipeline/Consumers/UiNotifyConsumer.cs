using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.Events;
using MediatR;

namespace IIoT.Edge.Tasks.DataPipeline.Consumers;

/// <summary>
/// UI 通知 + 产能统计消费者
/// 
/// 消费链的最后一环（Order=40）
/// 1. 将产能记录存入 SQLite
/// 2. 通过 MediatR 发布 CellCompletedEvent 通知 UI 刷新
/// </summary>
public class UiNotifyConsumer : IUiNotifyConsumer
{
    private readonly ICapacityRecordStore _capacityStore;
    private readonly IPublisher _publisher;
    private readonly ILogService _logger;

    public string Name => "UI";
    public int Order => 40;

    public UiNotifyConsumer(
        ICapacityRecordStore capacityStore,
        IPublisher publisher,
        ILogService logger)
    {
        _capacityStore = capacityStore;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        try
        {
            var cellData = record.CellData;

            // 1. 存产能记录
            var capacityRecord = new CapacityRecord
            {
                Barcode = cellData.DisplayLabel,
                CellResult = cellData.CellResult ?? false,
                ShiftCode = cellData.ShiftCode ?? GetShiftCode(cellData.CompletedTime ?? DateTime.Now),
                CompletedTime = cellData.CompletedTime ?? DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await _capacityStore.SaveAsync(capacityRecord);

            // 2. 发布事件通知 UI
            await _publisher.Publish(new CellCompletedEvent(record));

            var result = cellData.CellResult switch
            {
                true => "OK",
                false => "NG",
                _ => "未判定"
            };

            _logger.Info($"[UI] 产能已记录，{cellData.DisplayLabel}" +
                $"（{result}，班次: {capacityRecord.ShiftCode}）");

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[UI] 产能记录/通知失败，{record.CellData.DisplayLabel}，{ex.Message}");
            return false;
        }
    }

    private static string GetShiftCode(DateTime time)
    {
        var hour = time.Hour;
        return hour >= 8 && hour < 20 ? "白班" : "夜班";
    }
}