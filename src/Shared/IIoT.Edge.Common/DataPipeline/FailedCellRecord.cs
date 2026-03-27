namespace IIoT.Edge.Common.DataPipeline;

/// <summary>
/// 失败记录实体 — SQLite 中存储的待重传记录
/// 
/// 对应数据库：pipeline.db
/// 对应表：failed_cell_records
/// 
/// ProcessQueueTask 消费失败时写入：
///   CellData 序列化为 JSON 存入 CellDataJson
///   ProcessType 记录工序类型，反序列化时用于还原具体子类
/// 
/// RetryTask 捞出时：
///   根据 ProcessType 反序列化 CellDataJson → 具体的 CellDataBase 子类
///   包装成 CellCompletedRecord 继续消费
/// </summary>
public class FailedCellRecord
{
    public long Id { get; set; }

    /// <summary>
    /// 工序类型标识（"Injection"、"DieCutting" 等）
    /// 反序列化时用于识别 CellDataJson 的具体类型
    /// </summary>
    public string ProcessType { get; set; } = string.Empty;

    /// <summary>
    /// CellDataBase 序列化后的 JSON（SQLite 只能存字符串）
    /// </summary>
    public string CellDataJson { get; set; } = string.Empty;

    /// <summary>
    /// 在哪个消费者失败的（"Cloud" / "MES" / "Excel"）
    /// 重试时从这一步开始，之前成功的不重复
    /// </summary>
    public string FailedTarget { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime NextRetryTime { get; set; }
    public DateTime CreatedAt { get; set; }
}