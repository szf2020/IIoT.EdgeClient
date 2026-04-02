namespace IIoT.Edge.Module.Production.CapacityView;

/// <summary>
/// 产能列表行 ViewModel
/// 日查询：每行是一个半小时槽
/// 月查询：每行是一天
/// 年查询：每行是一个月
/// </summary>
public class DailyCapacityVm
{
    public string Date { get; set; } = "";
    public string DateFull { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public int Total { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public string Yield { get; set; } = "";
    public int DayShiftTotal { get; set; }
    public int DayShiftOk { get; set; }
    public int DayShiftNg { get; set; }
    public int NightShiftTotal { get; set; }
    public int NightShiftOk { get; set; }
    public int NightShiftNg { get; set; }
}

/// <summary>
/// 半小时槽 ViewModel（日查询明细用）
/// </summary>
public class HourlySlotVm
{
    public int SlotOrder { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public string TimeLabel { get; set; } = "";
    public string ShiftCode { get; set; } = "";
    public int TotalCount { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
}

/// <summary>
/// 日汇总 ViewModel（日查询兜底 / 月年聚合基础）
/// </summary>
public class DailySummaryVm
{
    public int TotalCount { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public int DayShiftTotal { get; set; }
    public int DayShiftOk { get; set; }
    public int DayShiftNg { get; set; }
    public int NightShiftTotal { get; set; }
    public int NightShiftOk { get; set; }
    public int NightShiftNg { get; set; }
}

/// <summary>
/// 图表柱状图 ViewModel
/// </summary>
public class CapacityChartBarVm
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
    public double HeightRatio { get; set; }
    public double ChartHeight { get; set; }
}