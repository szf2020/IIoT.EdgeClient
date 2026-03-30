using System;
using System.Collections.Generic;
using System.Text;

namespace IIoT.Edge.Common.DataPipeline.Capacity;

/// <summary>
/// 班次配置
/// 
/// 从 appsettings.json 的 "Shift" 节绑定
/// {
///   "Shift": {
///     "DayStart": "08:30",
///     "DayEnd": "20:30"
///   }
/// }
/// </summary>
public class ShiftConfig
{
    /// <summary>
    /// 白班开始时间，默认 08:30
    /// </summary>
    public string DayStart { get; set; } = "08:30";

    /// <summary>
    /// 白班结束时间（= 夜班开始时间），默认 20:30
    /// </summary>
    public string DayEnd { get; set; } = "20:30";

    /// <summary>
    /// 解析后的白班起始 TimeSpan
    /// </summary>
    public TimeSpan DayStartTime =>
        TimeSpan.TryParse(DayStart, out var ts) ? ts : new TimeSpan(8, 30, 0);

    /// <summary>
    /// 解析后的白班结束 TimeSpan
    /// </summary>
    public TimeSpan DayEndTime =>
        TimeSpan.TryParse(DayEnd, out var ts) ? ts : new TimeSpan(20, 30, 0);
}
