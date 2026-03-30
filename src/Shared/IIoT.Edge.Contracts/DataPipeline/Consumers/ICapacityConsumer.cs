namespace IIoT.Edge.Contracts.DataPipeline.Consumers;

/// <summary>
/// 产能统计消费者标记接口
/// 
/// DI 注册用：
///   services.AddSingleton&lt;ICapacityConsumer, CapacityConsumer&gt;();
///   services.AddSingleton&lt;ICellDataConsumer&gt;(sp => sp.GetRequiredService&lt;ICapacityConsumer&gt;());
/// </summary>
public interface ICapacityConsumer : ICellDataConsumer
{
}