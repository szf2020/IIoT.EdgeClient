namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// MES 上报消费者接口
/// 
/// 不同客户的 MES 系统完全不同：
///   - 数据格式、签名方式、接口地址各异
///   - 有的客户有入站/出站/补传，有的只有出站
/// 
/// 实现类按客户在 Tasks 层各自实现
/// </summary>
public interface IMesConsumer : ICellDataConsumer;