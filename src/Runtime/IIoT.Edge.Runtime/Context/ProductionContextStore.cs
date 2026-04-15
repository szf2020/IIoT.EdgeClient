using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.SharedKernel.Context;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IIoT.Edge.Runtime.Context;

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

    public ProductionContext GetOrCreate(string deviceName)
    {
        lock (_lock)
        {
            if (_contexts.TryGetValue(deviceName, out var ctx))
            {
                return ctx;
            }

            ctx = new ProductionContext
            {
                DeviceName = deviceName
            };
            _contexts[deviceName] = ctx;
            return ctx;
        }
    }

    public IReadOnlyCollection<ProductionContext> GetAll()
    {
        lock (_lock)
        {
            return _contexts.Values.ToList().AsReadOnly();
        }
    }

    public void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_persistPath))
            {
                _logger.Info("[ContextStore] No persisted file found. Using empty state.");
                return;
            }

            var json = File.ReadAllText(_persistPath);
            var list = JsonSerializer.Deserialize<List<ProductionContext>>(json, _jsonOptions);
            if (list is null)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var ctx in list)
                {
                    _contexts[ctx.DeviceName] = ctx;
                }
            }

            _logger.Info($"[ContextStore] Restored {list.Count} device contexts.");

            lock (_lock)
            {
                foreach (var ctx in _contexts.Values)
                {
                    var cellCount = ctx.CurrentCells.Count;
                    var stepInfo = string.Join(", ", ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));
                    var capacity = ctx.TodayCapacity;
                    _logger.Info(
                        $"  [{ctx.DeviceName}] Cells:{cellCount}, Steps:{(string.IsNullOrEmpty(stepInfo) ? "None" : stepInfo)}, DayShift:{capacity.DayShift.Total}, NightShift:{capacity.NightShift.Total}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[ContextStore] Load failed: {ex.Message}");
        }
    }

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

            _logger.Info($"[ContextStore] Saved {contexts.Count} device contexts.");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ContextStore] Save failed: {ex.Message}");
        }
    }

    public async Task StartAutoSaveAsync(CancellationToken ct, int intervalSeconds = 30)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                SaveToFile();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }
}

internal class CellDataBaseConverter : JsonConverter<CellDataBase>
{
    public override CellDataBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var processType = root.TryGetProperty("processType", out var prop)
            ? prop.GetString()
            : null;

        if (processType is null)
            throw new JsonException("CellData missing processType property.");

        var json = root.GetRawText();
        var result = CellDataTypeRegistry.Deserialize(processType, json, options);

        return result ?? throw new JsonException($"Unknown process type: {processType}");
    }

    public override void Write(Utf8JsonWriter writer, CellDataBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

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
