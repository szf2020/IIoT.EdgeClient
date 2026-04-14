namespace IIoT.Edge.SharedKernel.DataPipeline.Capacity;

/// <summary>
/// 当天产能快照。
/// 跟随 <c>ProductionContext</c> 一起持久化，日期归属按生产日规则计算。
/// </summary>
public class TodayCapacity
{
    /// <summary>
    /// 当前生产日日期，格式为 <c>yyyy-MM-dd</c>，用于跨天检测。
    /// </summary>
    public string Date { get; set; } = string.Empty;

    public ShiftCapacity DayShift { get; set; } = new("D");
    public ShiftCapacity NightShift { get; set; } = new("N");

    /// <summary>
    /// 半小时粒度的产能桶，覆盖 00:00-24:00 全时段。
    /// </summary>
    public List<HalfHourCapacity> HalfHourly { get; set; } = CreateHalfHourBuckets();

    // 汇总统计。
    public int TotalAll => DayShift.Total + NightShift.Total;
    public int OkAll => DayShift.OkCount + NightShift.OkCount;
    public int NgAll => DayShift.NgCount + NightShift.NgCount;
    public string YieldAll => TotalAll > 0
        ? $"{OkAll * 100.0 / TotalAll:F1}%"
        : "0%";

    /// <summary>
    /// 累加一次产能计数，并自动判定生产日与班次归属。
    /// </summary>
    /// <returns>本次归属的班次编码：D=白班，N=夜班。</returns>
    public string Increment(DateTime completedTime, bool isOk,
        TimeSpan dayStart, TimeSpan dayEnd)
    {
        // 生产日归属：00:00-DayStart 归属前一自然日。
        var productionDate = completedTime.TimeOfDay < dayStart
            ? completedTime.Date.AddDays(-1)
            : completedTime.Date;

        var currentDate = productionDate.ToString("yyyy-MM-dd");

        if (Date != currentDate)
        {
            Reset();
            Date = currentDate;
        }

        // 班次判定：白班为 DayStart-DayEnd，其余归夜班。
        var time = completedTime.TimeOfDay;
        var isDayShift = time >= dayStart && time < dayEnd;
        var shift = isDayShift ? DayShift : NightShift;

        if (isOk)
            shift.OkCount++;
        else
            shift.NgCount++;

        // 半小时桶归档。
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
    /// 清零当前快照，用于跨生产日或手动重置。
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
/// 单个班次的产能统计。
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

/// <summary>
/// 单个半小时产能桶。
/// </summary>
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
