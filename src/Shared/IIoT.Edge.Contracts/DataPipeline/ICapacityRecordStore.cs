using IIoT.Edge.Common.DataPipeline;

namespace IIoT.Edge.Contracts.DataPipeline;

/// <summary>
/// 产能汇总 DTO（对应界面上一行：日期 / 总产量 / 良品 / 不良 / 良率）
/// </summary>
public record DailySummaryDto(
    string Date,
    int Total,
    int OkCount,
    int NgCount
)
{
    public string DayOfWeek => System.DateTime.Parse(Date).ToString("ddd");
    public string Yield => Total > 0 ? $"{OkCount * 100.0 / Total:F1}%" : "0%";
}

/// <summary>
/// 产能记录存储接口
/// </summary>
public interface ICapacityRecordStore
{
    /// <summary>
    /// 插入一条产能记录
    /// </summary>
    Task SaveAsync(CapacityRecord record);

    /// <summary>
    /// 按天汇总（产能查询页面用）
    /// </summary>
    Task<List<DailySummaryDto>> GetDailySummaryAsync(DateTime dateFrom, DateTime dateTo);

    /// <summary>
    /// 获取区间总计（顶部汇总卡片用）
    /// </summary>
    Task<DailySummaryDto> GetPeriodSummaryAsync(DateTime dateFrom, DateTime dateTo);

    /// <summary>
    /// 获取总记录数（诊断用）
    /// </summary>
    Task<int> GetCountAsync();
}