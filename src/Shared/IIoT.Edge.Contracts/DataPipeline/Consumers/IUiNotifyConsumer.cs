namespace IIoT.Edge.Contracts.DataPipeline.Consumers;

/// <summary>
/// UI 通知 + 产能统计消费者接口
/// 
/// 通知 UI 刷新 + 记录产能（OK/NG）
/// </summary>
public interface IUiNotifyConsumer : ICellDataConsumer;