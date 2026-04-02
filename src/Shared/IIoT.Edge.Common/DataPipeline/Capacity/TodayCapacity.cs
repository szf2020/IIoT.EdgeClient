namespace IIoT.Edge.Common.DataPipeline.Capacity;

/// <summary>
/// 当天产能快照（内存模型）
///
/// 跟随 ProductionContext 一起 JSON 持久化
/// 日期归属按生产日（Production Day）计算：
///   生产日 08:30 开始，次日 08:30 结束
///   00:00-08:30 之间发生的生产归属前一个自然日
/// </summary>
public class TodayCapacity
{
    /// <summary>
    /// 当前生产日日期（yyyy-MM-dd），跨天检测用
    /// </summary>
    public string Date { get; set; } = string.Empty;

    public ShiftCapacity DayShift { get; set; } = new("D");
    public ShiftCapacity NightShift { get; set; } = new("N");

    /// <summary>
    /// 半小时产能（00:00-00:30 ... 23:30-24:00）
    /// </summary>
    public List<HalfHourCapacity> HalfHourly { get; set; } = CreateHalfHourBuckets();

    // ── 合计 ──────────────────────────────────────
    public int TotalAll => DayShift.Total + NightShift.Total;
    public int OkAll => DayShift.OkCount + NightShift.OkCount;
    public int NgAll => DayShift.NgCount + NightShift.NgCount;
    public string YieldAll => TotalAll > 0
        ? $"{OkAll * 100.0 / TotalAll:F1}%"
        : "0%";

    /// <summary>
    /// 计数 +1，自动判定班次，按生产日归属日期
    /// 00:00-DayStart 之间的生产归属前一自然日（仍是当晚夜班）
    /// </summary>
    /// <returns>本次归属的班次编码：D=白班, N=夜班</returns>
    public string Increment(DateTime completedTime, bool isOk,
        TimeSpan dayStart, TimeSpan dayEnd)
    {
        // ── 生产日归属：00:00-DayStart 归属前一自然日 ──────────────
        var productionDate = completedTime.TimeOfDay < dayStart
            ? completedTime.Date.AddDays(-1)
            : completedTime.Date;

        var currentDate = productionDate.ToString("yyyy-MM-dd");

        if (Date != currentDate)
        {
            Reset();
            Date = currentDate;
        }

        // ── 班次判定：白班 DayStart-DayEnd，其余夜班 ──────────────
        var time = completedTime.TimeOfDay;
        var isDayShift = time >= dayStart && time < dayEnd;
        var shift = isDayShift ? DayShift : NightShift;

        if (isOk)
            shift.OkCount++;
        else
            shift.NgCount++;

        // ── 半小时桶 ──────────────────────────────────────────────
        if (HalfHourly.Count == 0)
            HalfHourly = CreateHalfHourBuckets();

        var slotIndex = completedTime.Hour * 2 + (completedTime.Minute >= 30 ? 1 : 0);
        var bucket = HalfHourly.FirstOrDefault(x => x.SlotIndex == slotIndex);
        if (bucket is null)
        {
            bucket = CreateBucket(slotIndex);
            HalfHourly.Add(bucket);
            HalfHourly = HalfHourly.OrderBy(x => x.SlotIndex).ToList();
        }

        if (isOk)
            bucket.OkCount++;
        else
            bucket.NgCount++;

        return isDayShift ? "D" : "N";
    }

    /// <summary>
    /// 清零（跨生产日 / 手动重置）
    /// </summary>
    public void Reset()
    {
        DayShift = new ShiftCapacity("D");
        NightShift = new ShiftCapacity("N");
        HalfHourly = CreateHalfHourBuckets();
    }

    private static List<HalfHourCapacity> CreateHalfHourBuckets()
    {
        return Enumerable.Range(0, 48)
            .Select(CreateBucket)
            .ToList();
    }

    private static HalfHourCapacity CreateBucket(int slotIndex)
    {
        var startHour = slotIndex / 2;
        var startMinute = slotIndex % 2 == 0 ? 0 : 30;
        var endTotalMinutes = startHour * 60 + startMinute + 30;
        var endHour = (endTotalMinutes / 60) % 24;
        var endMinute = endTotalMinutes % 60;

        return new HalfHourCapacity
        {
            SlotIndex = slotIndex,
            StartHour = startHour,
            StartMinute = startMinute,
            EndHour = endHour,
            EndMinute = endMinute
        };
    }
}

/// <summary>
/// 单个班次的产能统计
/// </summary>
public class ShiftCapacity
{
    public ShiftCapacity() { }
    public ShiftCapacity(string shiftCode) { ShiftCode = shiftCode; }

    public string ShiftCode { get; set; } = string.Empty;
    public int OkCount { get; set; }
    public int NgCount { get; set; }

    public int Total => OkCount + NgCount;
    public string Yield => Total > 0
        ? $"{OkCount * 100.0 / Total:F1}%"
        : "0%";
}

public class HalfHourCapacity
{
    public int SlotIndex { get; set; }
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int EndHour { get; set; }
    public int EndMinute { get; set; }

    public int OkCount { get; set; }
    public int NgCount { get; set; }

    public int Total => OkCount + NgCount;
    public string Yield => Total > 0
        ? $"{OkCount * 100.0 / Total:F1}%"
        : "0%";

    public string TimeLabel =>
        $"{StartHour:D2}:{StartMinute:D2}-{EndHour:D2}:{EndMinute:D2}";
}