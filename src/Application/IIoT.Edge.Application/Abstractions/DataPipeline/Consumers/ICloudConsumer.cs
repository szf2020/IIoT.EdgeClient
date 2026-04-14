namespace IIoT.Edge.Application.Abstractions.DataPipeline.Consumers;

/// <summary>
/// 云端上报消费者接口。
///
/// 用于将电芯生产数据上报到云端平台。
/// </summary>
public interface ICloudConsumer : ICellDataConsumer;
