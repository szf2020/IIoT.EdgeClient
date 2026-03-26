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
            // 1. 存产能记录
            var capacityRecord = new CapacityRecord
            {
                Barcode = record.Barcode,
                CellResult = record.CellResult,
                ShiftCode = GetShiftCode(record.CompletedTime),
                CompletedTime = record.CompletedTime,
                CreatedAt = DateTime.Now
            };

            await _capacityStore.SaveAsync(capacityRecord);

            // 2. 发布事件通知 UI
            await _publisher.Publish(new CellCompletedEvent(record));

            _logger.Info($"[{record.DeviceName}] 产能已记录+UI已通知，条码: {record.Barcode}" +
                $"（{(record.CellResult ? "OK" : "NG")}，班次: {capacityRecord.ShiftCode}）");

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[{record.DeviceName}] UI通知/产能记录失败，条码: {record.Barcode}，{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 根据时间判断班次
    /// 白班：08:00 ~ 20:00
    /// 夜班：20:00 ~ 次日 08:00
    /// </summary>
    private static string GetShiftCode(DateTime time)
    {
        var hour = time.Hour;
        return hour >= 8 && hour < 20 ? "白班" : "夜班";
    }
}