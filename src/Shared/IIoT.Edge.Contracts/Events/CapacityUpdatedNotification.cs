using IIoT.Edge.Common.DataPipeline.Capacity;
using MediatR;

namespace IIoT.Edge.Contracts.Events;

/// <summary>
/// 产能变更通知
/// 
/// 发布方：CapacityConsumer（每个电芯完成时）
/// 订阅方：MonitorWidget / CapacityViewWidget（实时刷新产能显示）
/// 
/// 携带当天完整快照，订阅方直接绑定，不需要再查询
/// </summary>
public class CapacityUpdatedNotification : INotification
{
    public required TodayCapacity Snapshot { get; init; }
}