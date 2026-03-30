using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Contracts.DataPipeline.Stores;

namespace IIoT.Edge.Tasks.Context;

/// <summary>
/// 当天产能内存存储实现
/// 
/// 操作 ProductionContext.TodayCapacity 内存对象
/// 持久化跟随 ProductionContextStore 的 JSON 序列化（30秒自动保存 + 退出保存）
/// 
/// 班次分界点从 ShiftConfig（appsettings.json）读取
/// </summary>
public class TodayCapacityStore : ITodayCapacityStore
{
    private readonly ProductionContext _context;
    private readonly ShiftConfig _shiftConfig;

    public TodayCapacityStore(
        ProductionContext context,
        ShiftConfig shiftConfig)
    {
        _context = context;
        _shiftConfig = shiftConfig;
    }

    /// <summary>
    /// 产能 +1，返回班次编码
    /// </summary>
    public string Increment(DateTime completedTime, bool isOk)
    {
        return _context.TodayCapacity.Increment(
            completedTime, isOk,
            _shiftConfig.DayStartTime,
            _shiftConfig.DayEndTime);
    }

    /// <summary>
    /// 当天快照
    /// </summary>
    public TodayCapacity GetSnapshot()
    {
        return _context.TodayCapacity;
    }

    /// <summary>
    /// 手动清零
    /// </summary>
    public void Reset()
    {
        _context.TodayCapacity.Reset();
    }
}