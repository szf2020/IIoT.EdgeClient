using IIoT.Edge.Common.DataPipeline.Capacity;

namespace IIoT.Edge.Contracts.DataPipeline.Stores;

/// <summary>
/// 离线缓冲汇总 DTO（补传时按 日期+班次 汇总）
/// 
/// 用 class + 无参构造，Dapper 映射 SQLite 聚合结果需要
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
/// 离线缓冲小时汇总 DTO（补传时按 日期+小时+班次 汇总）
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
/// 只在 Offline 时写入，Online 后汇总补传，成功后清空
/// 不做长期存储，不给 UI 查历史数据
/// </summary>
public interface ICapacityBufferStore
{
    /// <summary>
    /// 写入一条离线缓冲记录
    /// </summary>
    Task SaveAsync(CapacityRecord record);

    /// <summary>
    /// 按日期+班次汇总（兼容旧补传）
    /// </summary>
    Task<List<BufferSummaryDto>> GetShiftSummaryAsync();

    /// <summary>
    /// 按日期+小时+班次汇总（小时补传）
    /// </summary>
    Task<List<BufferHourlySummaryDto>> GetHourlySummaryAsync();

    /// <summary>
    /// 补传成功后清空
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// 缓冲区记录数（诊断用）
    /// </summary>
    Task<int> GetCountAsync();
}