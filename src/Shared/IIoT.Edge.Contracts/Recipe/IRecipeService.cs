using IIoT.Edge.Common.DataPipeline.Recipe;

namespace IIoT.Edge.Contracts.Recipe;

/// <summary>
/// 配方服务接口
/// </summary>
public interface IRecipeService
{
    // ── 数据源 ───────────────────────────────────

    RecipeSource ActiveSource { get; }
    void SwitchSource(RecipeSource source);

    // ── 读取（任务层用）─────────────────────────

    /// <summary>按参数名获取配方参数（如"电压"）</summary>
    RecipeParam? GetParam(string name);

    /// <summary>获取所有参数</summary>
    IReadOnlyDictionary<string, RecipeParam> GetAllParams();

    // ── 元信息 ───────────────────────────────────

    RecipeData? ActiveRecipe { get; }
    RecipeData? CloudRecipe { get; }
    RecipeData? LocalRecipe { get; }

    // ── 云端拉取 ─────────────────────────────────

    Task<bool> PullFromCloudAsync();

    // ── 本地应急编辑 ─────────────────────────────

    /// <summary>修改/新增本地参数</summary>
    void SetLocalParam(string name, double? min, double? max, string unit);

    /// <summary>删除本地参数</summary>
    void RemoveLocalParam(string name);

    // ── 持久化 ───────────────────────────────────

    void LoadFromFile();
    void SaveToFile();

    // ── 事件 ─────────────────────────────────────

    event Action? RecipeChanged;
}