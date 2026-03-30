namespace IIoT.Edge.Common.DataPipeline.Capacity;

/// <summary>
/// 产能记录实体 — SQLite 存储
/// 
/// 每个电芯完成时记录一条，用于本地产量/良率统计和将来上报云端
/// </summary>
public class CapacityRecord
{
    public long Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool CellResult { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }
    public DateTime CreatedAt { get; set; }
}