using IIoT.Edge.Common.DataPipeline.CellData;

namespace IIoT.Edge.Tasks.Context;

/// <summary>
/// 单台设备的生产运行时上下文（全局对象，可被任意 Task 访问）
/// 
/// 两层数据结构：
/// 1. DeviceBag  — 设备级数据（工单号、状态机步骤等，不属于某个电芯）
/// 2. CurrentCells — 电芯级数据（按条码隔离，强类型对象，断点直接可看）
/// 
/// 电芯流转完成后从 CurrentCells 移除，只保留当前在制品
/// 
/// 调试体验：
///   断点展开 CurrentCells 即可看到每颗电芯的完整强类型数据
///   不需要翻 Dictionary，不需要记 key 名
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
    /// 当前在制电芯（key=条码, value=强类型电芯数据）
    /// 断点展开即可看到每颗电芯的完整数据
    /// </summary>
    public Dictionary<string, CellDataBase> CurrentCells { get; set; } = new();

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

    /// <summary>
    /// 添加一颗电芯到上下文（流程状态机创建强类型对象后调用）
    /// </summary>
    public void AddCell(string barcode, CellDataBase cellData)
    {
        CurrentCells[barcode] = cellData;
        DataChanged?.Invoke("CellAdded", barcode, cellData);
    }

    /// <summary>
    /// 获取电芯数据（返回基类）
    /// </summary>
    public CellDataBase? GetCell(string barcode)
        => CurrentCells.TryGetValue(barcode, out var cell) ? cell : null;

    /// <summary>
    /// 获取电芯数据（泛型，返回具体子类）
    /// 调试时断点直接看到子类的所有属性
    /// </summary>
    public T? GetCell<T>(string barcode) where T : CellDataBase
        => CurrentCells.TryGetValue(barcode, out var cell) && cell is T typed ? typed : null;

    /// <summary>
    /// 电芯是否存在
    /// </summary>
    public bool HasCell(string barcode)
        => CurrentCells.ContainsKey(barcode);

    /// <summary>
    /// 获取当前所有在制电芯的条码列表
    /// </summary>
    public IReadOnlyList<string> GetAllBarcodes()
        => CurrentCells.Keys.ToList().AsReadOnly();

    /// <summary>
    /// 电芯流转完成，移除数据释放空间
    /// </summary>
    public bool RemoveCell(string barcode)
    {
        var removed = CurrentCells.Remove(barcode);
        if (removed)
            DataChanged?.Invoke("CellRemoved", barcode, null);
        return removed;
    }

    // ── 调试辅助 ───────────────────────────────────────────

    /// <summary>
    /// VS 监视窗口展开可看到设备级数据快照
    /// </summary>
    public Dictionary<string, string> DebugDeviceView
        => DeviceBag.OrderBy(kv => kv.Key)
                    .ToDictionary(
                        kv => kv.Key,
                        kv => $"{kv.Value} ({kv.Value?.GetType().Name})");

    /// <summary>
    /// VS 监视窗口展开可看到所有电芯数据
    /// 直接是强类型，展开就能看到所有属性
    /// </summary>
    public IReadOnlyDictionary<string, CellDataBase> DebugCellView
        => CurrentCells;

    // ── 重置 ───────────────────────────────────────────────

    /// <summary>
    /// 清空所有电芯数据（不清设备级数据和步骤状态）
    /// </summary>
    public void ClearAllCells()
    {
        CurrentCells.Clear();
        DataChanged?.Invoke("AllCellsCleared", "", null);
    }

    /// <summary>
    /// 全部重置
    /// </summary>
    public void Reset()
    {
        DeviceBag.Clear();
        CurrentCells.Clear();
        StepStates.Clear();
    }
}