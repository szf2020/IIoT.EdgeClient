using IIoT.Edge.Common.DataPipeline.Recipe;
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Contracts.Recipe;
using System.IO;
using System.Text.Json;

namespace IIoT.Edge.CloudSync.Recipe;

public class RecipeService : IRecipeService
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly ICloudApiEndpointProvider _endpointProvider;
    private readonly IDeviceService _deviceService;
    private readonly ILogService _logger;
    private readonly string _recipeDir;

    private RecipeData? _cloudRecipe;
    private RecipeData? _localRecipe;
    private RecipeSource _activeSource = RecipeSource.Cloud;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RecipeService(
        ICloudHttpClient cloudHttp,
        ICloudApiEndpointProvider endpointProvider,
        IDeviceService deviceService,
        ILogService logger)
    {
        _cloudHttp = cloudHttp;
        _endpointProvider = endpointProvider;
        _deviceService = deviceService;
        _logger = logger;

        _recipeDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "data", "recipe");
        Directory.CreateDirectory(_recipeDir);
    }

    // ── 数据源 ───────────────────────────────────

    public RecipeSource ActiveSource => _activeSource;

    public void SwitchSource(RecipeSource source)
    {
        if (_activeSource == source) return;
        _activeSource = source;
        _logger.Info($"[Recipe] 数据源切换为: {source}");
        RecipeChanged?.Invoke();
    }

    // ── 读取 ─────────────────────────────────────

    public RecipeParam? GetParam(string name)
    {
        var recipe = ActiveRecipe;
        if (recipe is null) return null;
        return recipe.Parameters.TryGetValue(name, out var param) ? param : null;
    }

    public IReadOnlyDictionary<string, RecipeParam> GetAllParams()
    {
        var recipe = ActiveRecipe;
        return recipe?.Parameters ?? new Dictionary<string, RecipeParam>();
    }

    // ── 元信息 ───────────────────────────────────

    public RecipeData? ActiveRecipe => _activeSource == RecipeSource.Cloud
        ? _cloudRecipe : _localRecipe;

    public RecipeData? CloudRecipe => _cloudRecipe;
    public RecipeData? LocalRecipe => _localRecipe;

    // ── 云端拉取 ─────────────────────────────────

    public async Task<bool> PullFromCloudAsync()
    {
        var device = _deviceService.CurrentDevice;
        if (device is null)
        {
            _logger.Warn("[Recipe] 设备未寻址，无法拉取配方");
            return false;
        }

        if (_deviceService.CurrentState == NetworkState.Offline)
        {
            _logger.Warn("[Recipe] 网络离线，无法拉取配方");
            return false;
        }

        var url = _endpointProvider.BuildRecipeByDevicePath(device.DeviceId);
        var json = await _cloudHttp.GetAsync(url);

        if (json is null)
        {
            _logger.Error("[Recipe] 云端配方拉取失败");
            return false;
        }

        try
        {
            var recipe = ParseCloudResponse(json);
            if (recipe is null)
            {
                _logger.Warn("[Recipe] 云端返回数据为空或解析失败");
                return false;
            }

            _cloudRecipe = recipe;
            SaveSingleFile(_cloudRecipe, GetCloudFilePath());
            _logger.Info($"[Recipe] 云端配方拉取成功: {recipe.RecipeName} {recipe.Version}，" +
                $"参数 {recipe.Parameters.Count} 个");
            RecipeChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[Recipe] 配方解析异常: {ex.Message}");
            return false;
        }
    }

    // ── 本地应急编辑 ─────────────────────────────

    public void SetLocalParam(string name, double? min, double? max, string unit)
    {
        _localRecipe ??= new RecipeData
        {
            RecipeName = "本地应急配方",
            Version = "LOCAL",
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        _localRecipe.Parameters[name] = new RecipeParam
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Min = min,
            Max = max,
            Unit = unit
        };

        _localRecipe.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SaveSingleFile(_localRecipe, GetLocalFilePath());
        _logger.Info($"[Recipe] 本地配方修改: {name} [{min} ~ {max}] {unit}");

        if (_activeSource == RecipeSource.Local)
            RecipeChanged?.Invoke();
    }

    public void RemoveLocalParam(string name)
    {
        if (_localRecipe is null) return;

        if (_localRecipe.Parameters.Remove(name))
        {
            _localRecipe.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveSingleFile(_localRecipe, GetLocalFilePath());
            _logger.Info($"[Recipe] 本地配方删除参数: {name}");

            if (_activeSource == RecipeSource.Local)
                RecipeChanged?.Invoke();
        }
    }

    // ── 持久化 ───────────────────────────────────

    public void LoadFromFile()
    {
        _cloudRecipe = LoadSingleFile(GetCloudFilePath());
        _localRecipe = LoadSingleFile(GetLocalFilePath());

        var cloudCount = _cloudRecipe?.Parameters.Count ?? 0;
        var localCount = _localRecipe?.Parameters.Count ?? 0;
        _logger.Info($"[Recipe] 配方加载完成: 云端 {cloudCount} 个参数，本地 {localCount} 个参数");
    }

    public void SaveToFile()
    {
        if (_cloudRecipe is not null)
            SaveSingleFile(_cloudRecipe, GetCloudFilePath());
        if (_localRecipe is not null)
            SaveSingleFile(_localRecipe, GetLocalFilePath());
    }

    public event Action? RecipeChanged;

    // ── 私有方法 ─────────────────────────────────

    private string GetCloudFilePath() => Path.Combine(_recipeDir, "cloud_recipe.json");
    private string GetLocalFilePath() => Path.Combine(_recipeDir, "local_recipe.json");

    private RecipeData? LoadSingleFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RecipeData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.Error($"[Recipe] 文件加载失败 {path}: {ex.Message}");
            return null;
        }
    }

    private void SaveSingleFile(RecipeData data, string path)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.Error($"[Recipe] 文件保存失败 {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析云端响应
    /// 
    /// 云端 Controller 返回 Ok(result.Value)
    /// 实际 HTTP 响应体是直接的数组：
    /// [
    ///   {
    ///     "id": "7bcfce0c-...",
    ///     "recipeName": "test开发设备",
    ///     "version": "V1.0",
    ///     "status": "Active",
    ///     "processId": "e43cc1d2-...",
    ///     "deviceId": "bd014ffe-...",
    ///     "parametersJsonb": "[{\"id\":\"...\",\"name\":\"电压\",\"min\":2.3,\"max\":3.7,\"unit\":\"伏特\"}, ...]"
    ///   }
    /// ]
    /// </summary>
    private RecipeData? ParseCloudResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 定位到配方数组
        JsonElement recipeArray;
        if (root.ValueKind == JsonValueKind.Array)
        {
            // 云端直接返回数组
            recipeArray = root;
        }
        else if (root.ValueKind == JsonValueKind.Object &&
                 root.TryGetProperty("value", out var valEl) &&
                 valEl.ValueKind == JsonValueKind.Array)
        {
            // Result<T> 包装格式
            recipeArray = valEl;
        }
        else
        {
            return null;
        }

        if (recipeArray.GetArrayLength() == 0)
            return null;

        // 优先取 Status=Active 的配方
        JsonElement? activeElement = null;
        foreach (var item in recipeArray.EnumerateArray())
        {
            if (item.TryGetProperty("status", out var statusEl) &&
                statusEl.GetString() == "Active")
            {
                activeElement = item;
                break;
            }
        }
        var recipeEl = activeElement ?? recipeArray[0];

        var recipe = new RecipeData
        {
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        if (recipeEl.TryGetProperty("id", out var idEl))
            recipe.RecipeId = idEl.GetString() ?? "";
        if (recipeEl.TryGetProperty("recipeName", out var nameEl))
            recipe.RecipeName = nameEl.GetString() ?? "";
        if (recipeEl.TryGetProperty("version", out var verEl))
            recipe.Version = verEl.GetString() ?? "";
        if (recipeEl.TryGetProperty("status", out var statEl))
            recipe.Status = statEl.GetString() ?? "";

        // parametersJsonb 是 JSON 字符串，内部是数组
        if (recipeEl.TryGetProperty("parametersJsonb", out var paramsEl))
        {
            var paramsJson = paramsEl.GetString();
            if (!string.IsNullOrEmpty(paramsJson))
                recipe.Parameters = ParseParametersJsonb(paramsJson);
        }

        return recipe;
    }

    /// <summary>
    /// 解析 parametersJsonb
    /// 
    /// 格式：[{"id":"xxx","name":"电压","min":2.3,"max":3.7,"unit":"伏特"}, ...]
    /// 转成 Dictionary[name, RecipeParam]
    /// </summary>
    private Dictionary<string, RecipeParam> ParseParametersJsonb(string json)
    {
        var result = new Dictionary<string, RecipeParam>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var param = new RecipeParam();

                if (item.TryGetProperty("id", out var idEl))
                    param.Id = idEl.GetString() ?? "";
                if (item.TryGetProperty("name", out var nameEl))
                    param.Name = nameEl.GetString() ?? "";
                if (item.TryGetProperty("min", out var minEl) && minEl.ValueKind == JsonValueKind.Number)
                    param.Min = minEl.GetDouble();
                if (item.TryGetProperty("max", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number)
                    param.Max = maxEl.GetDouble();
                if (item.TryGetProperty("unit", out var unitEl))
                    param.Unit = unitEl.GetString() ?? "";

                if (!string.IsNullOrEmpty(param.Name))
                    result[param.Name] = param;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[Recipe] parametersJsonb 解析失败: {ex.Message}");
        }

        return result;
    }
}