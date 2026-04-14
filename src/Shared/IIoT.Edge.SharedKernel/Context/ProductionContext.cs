using IIoT.Edge.SharedKernel.DataPipeline.Capacity;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace IIoT.Edge.SharedKernel.Context;

/// <summary>
/// Single PLC device production runtime context.
///
/// DeviceName is the unique identifier across the entire chain.
/// Each PLC device has its own independent ProductionContext.
///
/// Thread-safe: all collections use ConcurrentDictionary for concurrent read/write.
/// </summary>
public class ProductionContext
{
    public string DeviceName { get; set; } = string.Empty;

    public int DeviceId { get; set; }

    public ConcurrentDictionary<string, int> StepStates { get; set; } = new();

    public ConcurrentDictionary<string, object> DeviceBag { get; set; } = new();

    public ConcurrentDictionary<string, CellDataBase> CurrentCells { get; set; } = new();

    public TodayCapacity TodayCapacity { get; set; } = new();

    public event Action<string, string, object?>? DataChanged;

    // Step states

    public int GetStep(string taskName)
        => StepStates.TryGetValue(taskName, out var step) ? step : 0;

    public void SetStep(string taskName, int step)
    {
        StepStates[taskName] = step;
        DataChanged?.Invoke("Step", taskName, step);
    }

    // Device-level data

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
        => DeviceBag.TryRemove(key, out _);

    // Cell operations

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
        var removed = CurrentCells.TryRemove(barcode, out _);
        if (removed)
            DataChanged?.Invoke("CellRemoved", barcode, null);
        return removed;
    }

    // Debug helpers

    [JsonIgnore]
    public Dictionary<string, string> DebugDeviceView
        => DeviceBag.OrderBy(kv => kv.Key)
                    .ToDictionary(
                        kv => kv.Key,
                        kv => $"{kv.Value} ({kv.Value?.GetType().Name})");

    [JsonIgnore]
    public IReadOnlyDictionary<string, CellDataBase> DebugCellView
        => CurrentCells;

    // Reset

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
