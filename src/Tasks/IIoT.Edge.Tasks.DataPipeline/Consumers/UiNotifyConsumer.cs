using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.Events;
using MediatR;

namespace IIoT.Edge.Tasks.DataPipeline.Consumers;

/// <summary>
/// UI 通知消费者
/// 
/// 消费链最后一环（Order=50）
/// 职责单一：通过 MediatR 发布 CellCompletedEvent 通知 UI 刷新
/// 
/// 产能统计已移至 CapacityConsumer（Order=10）
/// </summary>
public class UiNotifyConsumer : IUiNotifyConsumer
{
    private readonly IPublisher _publisher;
    private readonly ILogService _logger;

    public string Name => "UI";
    public int Order => 50;
    public string? RetryChannel => null;

    public UiNotifyConsumer(
        IPublisher publisher,
        ILogService logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        try
        {
            await _publisher.Publish(new CellCompletedEvent(record));
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[UI] 通知发布失败，{record.CellData.DisplayLabel}，{ex.Message}");
            return true; // UI 通知失败不阻塞，不补传
        }
    }
}