namespace IIoT.Edge.Contracts.DataPipeline.Consumers;

/// <summary>
/// Excel 本地存储消费者接口
/// 
/// 不同机台可能有不同的 Excel 格式需求：
///   - 叠片机：条码 + 扫码数据 + 电压数据
///   - 卷绕机：条码 + 张力数据 + 温度数据
///   - 某些客户要求特殊的列顺序、表头名称、多 Sheet 等
/// 
/// 实现类在各自的基础设施类库中
/// </summary>
public interface IExcelConsumer : ICellDataConsumer;