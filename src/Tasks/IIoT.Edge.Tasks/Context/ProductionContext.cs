// 路径：src/Infrastructure/IIoT.Edge.Tasks/Context/ProductionContext.cs
using System.Collections.Concurrent;

namespace IIoT.Edge.Tasks.Context;

/// <summary>
/// 单台设备的生产运行时上下文（通用数据容器）
/// 
/// 两层数据结构：
/// 1. DeviceBag — 设备级数据（工单号、状态机步骤等，不属于某个电芯）
/// 2. CellBags — 电芯级数据（按条码隔离，每个电芯一个独立的数据空间）
/// 
/// 电芯流转完成后从 CellBags 移除，只保留当前在制品
/// </summary>
public class ProductionContext
{
    /// <summary>
    /// 所属设备ID（对应 NetworkDeviceEntity.Id）
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// 设备名称（日志/UI用）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 各任务的状态机当前步骤（key=TaskName, value=Step）
    /// </summary>
    public Dictionary<string, int> StepStates { get; set; } = new();

    /// <summary>
    /// 设备级数据（工单号、工站编码等，不属于某个电芯的数据）
    /// </summary>
    public Dictionary<string, object> DeviceBag { get; set; } = new();

    /// <summary>
    /// 电芯级数据（key=条码, value=该电芯的所有参数）
    /// 条码是电芯的唯一标识，所有任务按条码写入各自的数据
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> CellBags { get; set; } = new();

    /// <summary>
    /// 数据变更事件（UI绑定刷新用）
    /// 参数：(变更类型, key, value)
    /// </summary>
    public event Action<string, string, object?>? DataChanged;

    // ── 状态机步骤 ─────────────────────────────────────

    public int GetStep(string taskName)
        => StepStates.TryGetValue(taskName, out var step) ? step : 0;

    public void SetStep(string taskName, int step)
    {
        StepStates[taskName] = step;
        DataChanged?.Invoke("Step", taskName, step);
    }

    // ── 设备级数据存取 ─────────────────────────────────

    public void Set<T>(string key, T value)
    {
        DeviceBag[key] = value!;
        DataChanged?.Invoke("Device", key, value);
    }

    public T? Get<T>(string key)
        => DeviceBag.TryGetValue(key, out var val) && val is T t ? t : default;

    public bool Has(string key)
        => DeviceBag.ContainsKey(key);

    public bool Remove(string key)
        => DeviceBag.Remove(key);

    // ── 电芯级数据存取 ─────────────────────────────────

    /// <summary>
    /// 创建电芯数据空间（扫码任务扫到新条码时调用）
    /// </summary>
    public void CreateCell(string barcode)
    {
        if (!CellBags.ContainsKey(barcode))
        {
            CellBags[barcode] = new Dictionary<string, object>();
            DataChanged?.Invoke("CellCreated", barcode, null);
        }
    }

    /// <summary>
    /// 往指定条码的电芯写入数据
    /// </summary>
    public void SetCell<T>(string barcode, string key, T value)
    {
        if (!CellBags.ContainsKey(barcode))
            CreateCell(barcode);

        CellBags[barcode][key] = value!;
        DataChanged?.Invoke("Cell", $"{barcode}.{key}", value);
    }

    /// <summary>
    /// 从指定条码的电芯读取数据
    /// </summary>
    public T? GetCell<T>(string barcode, string key)
    {
        if (CellBags.TryGetValue(barcode, out var bag)
            && bag.TryGetValue(key, out var val)
            && val is T t)
            return t;
        return default;
    }

    /// <summary>
    /// 获取指定电芯的全部数据（出料组装时用）
    /// </summary>
    public Dictionary<string, object>? GetCellBag(string barcode)
        => CellBags.TryGetValue(barcode, out var bag) ? bag : null;

    /// <summary>
    /// 获取当前所有在制电芯的条码列表
    /// </summary>
    public IReadOnlyList<string> GetAllCellBarcodes()
        => CellBags.Keys.ToList().AsReadOnly();

    /// <summary>
    /// 电芯流转完成，移除数据释放空间
    /// </summary>
    public bool RemoveCell(string barcode)
    {
        var removed = CellBags.Remove(barcode);
        if (removed)
            DataChanged?.Invoke("CellRemoved", barcode, null);
        return removed;
    }

    // ── 调试辅助 ───────────────────────────────────────

    /// <summary>
    /// VS监视窗口展开可看到设备级数据快照
    /// </summary>
    public Dictionary<string, string> DebugDeviceView
        => DeviceBag.OrderBy(kv => kv.Key)
                    .ToDictionary(
                        kv => kv.Key,
                        kv => $"{kv.Value} ({kv.Value?.GetType().Name})");

    /// <summary>
    /// VS监视窗口展开可看到所有电芯数据快照
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> DebugCellView
        => CellBags.ToDictionary(
            cell => cell.Key,
            cell => cell.Value.OrderBy(kv => kv.Key)
                              .ToDictionary(
                                  kv => kv.Key,
                                  kv => $"{kv.Value} ({kv.Value?.GetType().Name})"));

    // ── 重置 ───────────────────────────────────────────

    /// <summary>
    /// 清空所有电芯数据（不清设备级数据和步骤状态）
    /// </summary>
    public void ClearAllCells()
    {
        CellBags.Clear();
        DataChanged?.Invoke("AllCellsCleared", "", null);
    }

    /// <summary>
    /// 全部重置
    /// </summary>
    public void Reset()
    {
        DeviceBag.Clear();
        CellBags.Clear();
        StepStates.Clear();
    }
}