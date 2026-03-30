namespace IIoT.Edge.Common.DataPipeline.Capacity;

/// <summary>
/// 当天产能快照（内存模型）
/// 
/// 跟随 ProductionContext 一起 JSON 持久化
/// 跨天时自动清零（Increment 前检查 Date）
/// </summary>
public class TodayCapacity
{
    /// <summary>
    /// 当前统计日期（yyyy-MM-dd），跨天检测用
    /// </summary>
    public string Date { get; set; } = string.Empty;

    public ShiftCapacity DayShift { get; set; } = new("D");
    public ShiftCapacity NightShift { get; set; } = new("N");

    // ── 合计 ──────────────────────────────────────
    public int TotalAll => DayShift.Total + NightShift.Total;
    public int OkAll => DayShift.OkCount + NightShift.OkCount;
    public int NgAll => DayShift.NgCount + NightShift.NgCount;
    public string YieldAll => TotalAll > 0
        ? $"{OkAll * 100.0 / TotalAll:F1}%"
        : "0%";

    /// <summary>
    /// 计数 +1，自动判定班次，自动跨天清零
    /// </summary>
    /// <returns>本次归属的班次编码："D"=白班, "N"=夜班</returns>
    public string Increment(DateTime completedTime, bool isOk,
        TimeSpan dayStart, TimeSpan dayEnd)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        if (Date != today)
        {
            Reset();
            Date = today;
        }

        var time = completedTime.TimeOfDay;
        var isDayShift = time >= dayStart && time < dayEnd;
        var shift = isDayShift ? DayShift : NightShift;

        if (isOk)
            shift.OkCount++;
        else
            shift.NgCount++;

        return isDayShift ? "D" : "N";
    }

    /// <summary>
    /// 清零（跨天 / 手动重置）
    /// </summary>
    public void Reset()
    {
        DayShift = new ShiftCapacity("D");
        NightShift = new ShiftCapacity("N");
    }
}

/// <summary>
/// 单个班次的产能统计
/// </summary>
public class ShiftCapacity
{
    public ShiftCapacity() { }
    public ShiftCapacity(string shiftCode) { ShiftCode = shiftCode; }

    /// <summary>
    /// 班次编码：D=白班, N=夜班
    /// </summary>
    public string ShiftCode { get; set; } = string.Empty;

    public int OkCount { get; set; }
    public int NgCount { get; set; }

    public int Total => OkCount + NgCount;
    public string Yield => Total > 0
        ? $"{OkCount * 100.0 / Total:F1}%"
        : "0%";
}