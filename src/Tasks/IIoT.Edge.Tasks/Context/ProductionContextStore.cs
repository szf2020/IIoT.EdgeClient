// 路径：src/Infrastructure/IIoT.Edge.Tasks/Context/ProductionContextStore.cs
using IIoT.Edge.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IIoT.Edge.Tasks.Context;

/// <summary>
/// 生产上下文仓库（IOC 单例注册）
/// 管理所有设备的 ProductionContext，负责持久化和恢复
/// </summary>
public class ProductionContextStore
{
    private readonly Dictionary<int, ProductionContext> _contexts = new();
    private readonly ILogService _logger;
    private readonly string _persistPath;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new ObjectToInferredTypesConverter() }
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
    public ProductionContext GetOrCreate(int deviceId, string deviceName = "")
    {
        lock (_lock)
        {
            if (_contexts.TryGetValue(deviceId, out var ctx))
                return ctx;

            ctx = new ProductionContext
            {
                DeviceId = deviceId,
                DeviceName = deviceName
            };
            _contexts[deviceId] = ctx;
            return ctx;
        }
    }

    /// <summary>
    /// 获取所有 Context（UI 展示用）
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
            var list = JsonSerializer.Deserialize<List<ContextSnapshot>>(json, _jsonOptions);

            if (list is null) return;

            lock (_lock)
            {
                foreach (var snapshot in list)
                {
                    var ctx = new ProductionContext
                    {
                        DeviceId = snapshot.DeviceId,
                        DeviceName = snapshot.DeviceName,
                        StepStates = snapshot.StepStates ?? new(),
                        DeviceBag = snapshot.DeviceBag ?? new(),
                        CellBags = snapshot.CellBags ?? new()
                    };
                    _contexts[ctx.DeviceId] = ctx;
                }
            }

            _logger.Info($"[ContextStore] 已恢复 {list.Count} 台设备的生产上下文");

            // 打印恢复摘要，方便排查
            lock (_lock)
            {
                foreach (var ctx in _contexts.Values)
                {
                    var cellCount = ctx.CellBags.Count;
                    var stepInfo = string.Join(", ",
                        ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));

                    _logger.Info($"  [{ctx.DeviceName}] " +
                        $"在制电芯: {cellCount}个, " +
                        $"步骤: {(string.IsNullOrEmpty(stepInfo) ? "无" : stepInfo)}");
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
    /// </summary>
    public void SaveToFile()
    {
        try
        {
            List<ContextSnapshot> snapshots;
            lock (_lock)
            {
                snapshots = _contexts.Values.Select(ctx => new ContextSnapshot
                {
                    DeviceId = ctx.DeviceId,
                    DeviceName = ctx.DeviceName,
                    StepStates = ctx.StepStates,
                    DeviceBag = ctx.DeviceBag,
                    CellBags = ctx.CellBags
                }).ToList();
            }

            var json = JsonSerializer.Serialize(snapshots, _jsonOptions);

            // 先写临时文件再替换，防止写到一半崩溃导致文件损坏
            var tempPath = _persistPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _persistPath, overwrite: true);

            _logger.Info($"[ContextStore] 已保存 {snapshots.Count} 台设备的生产上下文");
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
/// 持久化用的快照结构（与 ProductionContext 字段对应，但不携带事件等运行时对象）
/// </summary>
internal class ContextSnapshot
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Dictionary<string, int> StepStates { get; set; } = new();
    public Dictionary<string, object> DeviceBag { get; set; } = new();
    public Dictionary<string, Dictionary<string, object>> CellBags { get; set; } = new();
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