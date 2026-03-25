using IIoT.Edge.Common.DataPipeline;
using MediatR;

namespace IIoT.Edge.Contracts.Events;

/// <summary>
/// 电芯完成事件（本地发布/订阅）
/// 
/// 由 UiNotifyConsumer 在数据管道消费成功后发布
/// 订阅方：
///   1. UI 层 Handler — 刷新界面数据
///   2. 产能统计 Handler — 记录 OK/NG 计数
///   3. 将来 MQ Publisher — 推送到云端消息总线
/// 
/// 实现 INotification 表示这是一个通知型事件，可以有多个 Handler 订阅
/// 与 ICommand/IQuery 的区别：Command 只有一个 Handler，Notification 可以有多个
/// </summary>
public record CellCompletedEvent(CellCompletedRecord Record) : INotification;