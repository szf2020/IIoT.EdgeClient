using IIoT.Edge.Common.Context;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Context;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IIoT.Edge.Tasks.Context;

/// <summary>
/// 生产上下文仓库（IOC 单例注册）
/// 
/// 按 DeviceName（设备名）管理所有 PLC 设备的 ProductionContext
/// DeviceName 是全局唯一聚合 key，贯穿 PLC任务 → CellData → 消费链 → 云端上传
/// 
/// 持久化：直接序列化整个 ProductionContext，不需要中间快照结构
/// </summary>
public class ProductionContextStore : IProductionContextStore
{
    private readonly Dictionary<string, ProductionContext> _contexts = new();
    private readonly ILogService _logger;
    private readonly string _persistPath;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new ObjectToInferredTypesConverter(),
            new CellDataBaseConverter()
        }
    };

    public ProductionContextStore(ILogService logger, string? persistDirectory = null)
    {
        _logger = logger;

        var dir = persistDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IIoT.Edge");

        Directory.CreateDirectory(dir);
        _persistPath = Path.Combine(dir, "production_context.json");
    }

    /// <summary>
    /// 获取指定设备的 Context，不存在则创建
    /// </summary>
    /// <param name="deviceName">设备名（唯一 key，如"注液机1"）</param>
    public ProductionContext GetOrCreate(string deviceName)
    {
        lock (_lock)
        {
            if (_contexts.TryGetValue(deviceName, out var ctx))
                return ctx;

            ctx = new ProductionContext
            {
                DeviceName = deviceName
            };
            _contexts[deviceName] = ctx;
            return ctx;
        }
    }

    /// <summary>
    /// 获取所有 Context（UI 展示 / CapacitySyncTask 遍历用）
    /// </summary>
    public IReadOnlyCollection<ProductionContext> GetAll()
    {
        lock (_lock)
        {
            return _contexts.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// 从本地文件恢复所有设备的状态（启动时调用）
    /// </summary>
    public void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_persistPath))
            {
                _logger.Info("[ContextStore] 无持久化文件，使用初始状态");
                return;
            }

            var json = File.ReadAllText(_persistPath);
            var list = JsonSerializer.Deserialize<List<ProductionContext>>(json, _jsonOptions);

            if (list is null) return;

            lock (_lock)
            {
                foreach (var ctx in list)
                {
                    _contexts[ctx.DeviceName] = ctx;
                }
            }

            _logger.Info($"[ContextStore] 已恢复 {list.Count} 台设备的生产上下文");

            lock (_lock)
            {
                foreach (var ctx in _contexts.Values)
                {
                    var cellCount = ctx.CurrentCells.Count;
                    var stepInfo = string.Join(", ",
                        ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));
                    var capacity = ctx.TodayCapacity;

                    _logger.Info($"  [{ctx.DeviceName}] " +
                        $"在制电芯: {cellCount}个, " +
                        $"步骤: {(string.IsNullOrEmpty(stepInfo) ? "无" : stepInfo)}, " +
                        $"当天产能: 白班={capacity.DayShift.Total}/夜班={capacity.NightShift.Total}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[ContextStore] 加载持久化文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 将所有设备状态写入本地文件（关闭时调用）
    /// 直接序列化 ProductionContext，不需要中间快照
    /// </summary>
    public void SaveToFile()
    {
        try
        {
            List<ProductionContext> contexts;
            lock (_lock)
            {
                contexts = _contexts.Values.ToList();
            }

            var json = JsonSerializer.Serialize(contexts, _jsonOptions);

            var tempPath = _persistPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _persistPath, overwrite: true);

            _logger.Info($"[ContextStore] 已保存 {contexts.Count} 台设备的生产上下文");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ContextStore] 持久化写入失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 定时自动保存（防崩溃丢数据）
    /// </summary>
    public async Task StartAutoSaveAsync(CancellationToken ct, int intervalSeconds = 30)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
            SaveToFile();
        }
    }
}

/// <summary>
/// CellDataBase 多态 JSON 序列化/反序列化
/// </summary>
internal class CellDataBaseConverter : JsonConverter<CellDataBase>
{
    public override CellDataBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var processType = root.TryGetProperty("processType", out var prop)
            ? prop.GetString()
            : null;

        var json = root.GetRawText();

        return processType switch
        {
            "Injection" => JsonSerializer.Deserialize<InjectionCellData>(json, options),
            _ => throw new JsonException($"未知的 ProcessType: {processType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, CellDataBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

/// <summary>
/// JSON 反序列化时将 JsonElement 还原为基础类型
/// </summary>
public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt32(out var i) => i,
            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out var dt) => dt,
            JsonTokenType.String => reader.GetString()!,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}