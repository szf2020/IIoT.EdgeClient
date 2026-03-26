namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// 云端上报消费者接口
/// 
/// 将电芯数据上报到自研云端平台
/// </summary>
public interface ICloudConsumer : ICellDataConsumer;