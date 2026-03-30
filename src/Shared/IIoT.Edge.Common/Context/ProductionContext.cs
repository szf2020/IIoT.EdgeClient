using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Common.DataPipeline.CellData;
using System.Text.Json.Serialization;

namespace IIoT.Edge.Common.Context;

/// <summary>
/// 单台 PLC 设备的生产运行时上下文
/// 
/// DeviceName 是唯一标识（聚合 key），贯穿全链路
/// 一个 PLC 设备 = 一个 ProductionContext
/// 
/// 数据结构：
/// 1. DeviceBag    — 设备级数据（工单号、状态机步骤等）
/// 2. CurrentCells — 电芯级数据（按条码隔离，强类型）
/// 3. TodayCapacity — 当天产能快照（白班/夜班）
/// </summary>
public class ProductionContext
{
    /// <summary>
    /// 本地设备名（唯一 key，如"注液机1"）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// PLC 设备数据库主键（NetworkDeviceEntity.Id）
    /// 非聚合 key，仅供需要数据库关联的场景使用
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// 各任务的状态机当前步骤（key=TaskName, value=Step）
    /// </summary>
    public Dictionary<string, int> StepStates { get; set; } = new();

    /// <summary>
    /// 设备级数据（工单号、工站编码等，不属于某个电芯的数据）
    /// </summary>
    public Dictionary<string, object> DeviceBag { get; set; } = new();

    /// <summary>
    /// 当前在制电芯（key=条码, value=强类型电芯数据）
    /// </summary>
    public Dictionary<string, CellDataBase> CurrentCells { get; set; } = new();

    /// <summary>
    /// 当天产能快照（白班/夜班分别统计）
    /// </summary>
    public TodayCapacity TodayCapacity { get; set; } = new();

    /// <summary>
    /// 数据变更事件（UI绑定刷新用）
    /// </summary>
    public event Action<string, string, object?>? DataChanged;

    // ── 状态机步骤 ─────────────────────────────────────────

    public int GetStep(string taskName)
        => StepStates.TryGetValue(taskName, out var step) ? step : 0;

    public void SetStep(string taskName, int step)
    {
        StepStates[taskName] = step;
        DataChanged?.Invoke("Step", taskName, step);
    }

    // ── 设备级数据存取 ─────────────────────────────────────

    public void Set<T>(string key, T value)
    {
        DeviceBag[key] = value!;
        DataChanged?.Invoke("Device", key, value);
    }

    public T? Get<T>(string key)
        => DeviceBag.TryGetValue(key, out var val) && val is T t ? t : default;

    public bool Has(string key)
        => DeviceBag.ContainsKey(key);

    public bool RemoveDeviceData(string key)
        => DeviceBag.Remove(key);

    // ── 电芯操作 ───────────────────────────────────────────

    public void AddCell(string barcode, CellDataBase cellData)
    {
        CurrentCells[barcode] = cellData;
        DataChanged?.Invoke("CellAdded", barcode, cellData);
    }

    public CellDataBase? GetCell(string barcode)
        => CurrentCells.TryGetValue(barcode, out var cell) ? cell : null;

    public T? GetCell<T>(string barcode) where T : CellDataBase
        => CurrentCells.TryGetValue(barcode, out var cell) && cell is T typed ? typed : null;

    public bool HasCell(string barcode)
        => CurrentCells.ContainsKey(barcode);

    public IReadOnlyList<string> GetAllBarcodes()
        => CurrentCells.Keys.ToList().AsReadOnly();

    public bool RemoveCell(string barcode)
    {
        var removed = CurrentCells.Remove(barcode);
        if (removed)
            DataChanged?.Invoke("CellRemoved", barcode, null);
        return removed;
    }

    // ── 调试辅助 ───────────────────────────────────────────

    [JsonIgnore]
    public Dictionary<string, string> DebugDeviceView
        => DeviceBag.OrderBy(kv => kv.Key)
                    .ToDictionary(
                        kv => kv.Key,
                        kv => $"{kv.Value} ({kv.Value?.GetType().Name})");

    [JsonIgnore]
    public IReadOnlyDictionary<string, CellDataBase> DebugCellView
        => CurrentCells;

    // ── 重置 ───────────────────────────────────────────────

    public void ClearAllCells()
    {
        CurrentCells.Clear();
        DataChanged?.Invoke("AllCellsCleared", "", null);
    }

    public void Reset()
    {
        DeviceBag.Clear();
        CurrentCells.Clear();
        StepStates.Clear();
        TodayCapacity.Reset();
    }
}