using IIoT.Edge.Common.DataPipeline.Capacity;

namespace IIoT.Edge.Contracts.DataPipeline.Stores;

/// <summary>
/// 离线缓冲汇总 DTO（按日期+班次汇总）
/// </summary>
public class BufferSummaryDto
{
    public string Date { get; set; } = string.Empty;
    public string ShiftCode { get; set; } = string.Empty;
    public int Total { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
}

/// <summary>
/// 离线缓冲小时汇总 DTO（按日期+小时+分钟桶+班次汇总）
/// </summary>
public class BufferHourlySummaryDto
{
    public string Date { get; set; } = string.Empty;
    public int Hour { get; set; }
    public int MinuteBucket { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public int Total { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
}

/// <summary>
/// 产能离线缓冲接口
///
/// 只在 Offline 时写入，Online 后聚合补传，成功后清空
/// 不做长期存储，不给 UI 查历史数据
/// </summary>
public interface ICapacityBufferStore
{
    /// <summary>
    /// 写入单条离线缓冲记录（实时写入，每个电芯完成时调用）
    /// </summary>
    Task SaveAsync(CapacityRecord record);

    /// <summary>
    /// 批量写入离线缓冲记录（事务批量插入，历史数据生成/大批量场景使用）
    /// </summary>
    Task SaveBatchAsync(IEnumerable<CapacityRecord> records);

    /// <summary>
    /// 按日期+班次汇总（兼容旧补传）
    /// </summary>
    Task<List<BufferSummaryDto>> GetShiftSummaryAsync();

    /// <summary>
    /// 按日期+小时+分钟桶+班次汇总（补传时用，聚合后逐槽 POST 云端）
    /// </summary>
    Task<List<BufferHourlySummaryDto>> GetHourlySummaryAsync();

    /// <summary>
    /// 补传成功后清空所有缓冲
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// 缓冲区记录数（诊断用）
    /// </summary>
    Task<int> GetCountAsync();
}