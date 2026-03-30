using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Contracts.Events;
using MediatR;

namespace IIoT.Edge.CloudSync.Capacity;

/// <summary>
/// 产能统计消费者
/// 
/// 消费链 Order=10（最先执行）
/// 每个电芯完成时：
///   1. ITodayCapacityStore.Increment — 内存计数 +1
///   2. MediatR 发布 CapacityUpdatedNotification — UI 实时刷新
///   3. Offline 时写 SQLite 离线缓冲 — 待补传
/// 
/// 永远返回 true，不进重传队列
/// </summary>
public class CapacityConsumer : ICapacityConsumer
{
    private readonly ITodayCapacityStore _capacityStore;
    private readonly IDeviceService _deviceService;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly IPublisher _publisher;
    private readonly ILogService _logger;

    public string Name => "Capacity";
    public int Order => 10;
    public string? RetryChannel => null;

    public CapacityConsumer(
        ITodayCapacityStore capacityStore,
        IDeviceService deviceService,
        ICapacityBufferStore bufferStore,
        IPublisher publisher,
        ILogService logger)
    {
        _capacityStore = capacityStore;
        _deviceService = deviceService;
        _bufferStore = bufferStore;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        try
        {
            var cellData = record.CellData;
            var completedTime = cellData.CompletedTime ?? DateTime.Now;
            var isOk = cellData.CellResult ?? false;

            // 1. 内存计数 +1，返回班次编码
            var shiftCode = _capacityStore.Increment(completedTime, isOk);

            // 2. MediatR 通知 UI 实时刷新
            var snapshot = _capacityStore.GetSnapshot();
            await _publisher.Publish(new CapacityUpdatedNotification
            {
                Snapshot = snapshot
            });

            // 3. 离线时写 SQLite 缓冲（待网络恢复后补传）
            if (_deviceService.CurrentState == NetworkState.Offline)
            {
                await _bufferStore.SaveAsync(new CapacityRecord
                {
                    Barcode = cellData.DisplayLabel,
                    CellResult = isOk,
                    ShiftCode = shiftCode,
                    CompletedTime = completedTime,
                    CreatedAt = DateTime.Now
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[Capacity] 产能统计异常: {ex.Message}");
            return true;
        }
    }
}