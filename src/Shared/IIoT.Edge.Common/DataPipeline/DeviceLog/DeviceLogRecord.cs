using System;
using System.Collections.Generic;
using System.Text;

namespace IIoT.Edge.Common.DataPipeline.DeviceLog;

/// <summary>
/// 设备日志 SQLite 离线缓冲记录
/// 
/// POST /api/v1/DeviceLog 失败或离线时写入
/// RetryTask[Cloud] 补传时捞出来批量 POST
/// </summary>
public class DeviceLogRecord
{
    public long Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string LogTime { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
