namespace IIoT.Edge.Common.DataPipeline;

/// <summary>
/// 失败记录实体 — SQLite 中存储的待重传记录
/// 
/// 对应数据库：pipeline.db
/// 对应表：failed_cell_records
/// 
/// ProcessQueueTask 消费失败时写入
/// RetryTask 定时捞出重试，成功则删除
/// </summary>
public class FailedCellRecord
{
    public long Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int LocalDeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? CloudDeviceCode { get; set; }
    public bool CellResult { get; set; }
    public string DataJson { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }

    /// <summary>
    /// 在哪个消费者失败的（"MES" / "Cloud" / "Excel"）
    /// 重试时从这一步开始，之前成功的不重复
    /// </summary>
    public string FailedTarget { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime NextRetryTime { get; set; }
    public DateTime CreatedAt { get; set; }
}