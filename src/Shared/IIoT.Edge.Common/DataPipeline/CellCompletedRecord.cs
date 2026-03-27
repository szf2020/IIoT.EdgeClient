using IIoT.Edge.Common.DataPipeline.CellData;

namespace IIoT.Edge.Common.DataPipeline;

/// <summary>
/// 电芯完成记录 — 进入数据管道的载体
/// 
/// 组装确认 Task 判定 OK/NG 后，将强类型 CellData 包装成此对象
/// 推入内存队列，由 ProcessQueueTask 按顺序消费
/// 
/// 消费者直接读取 CellData 的强类型属性，不需要解析 JSON
/// </summary>
public class CellCompletedRecord
{
    /// <summary>
    /// 电芯生产数据（强类型，具体子类由工序决定）
    /// 消费者可通过 is/as 或 ProcessType 判断具体类型
    /// </summary>
    public CellDataBase CellData { get; set; } = null!;
}