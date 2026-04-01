namespace IIoT.Edge.Common.DataPipeline.Recipe;

public enum RecipeSource
{
    Cloud,
    Local
}

/// <summary>
/// 配方内存模型
/// </summary>
public class RecipeData
{
    public string RecipeId { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// 配方参数列表
    /// key = 参数名（如"电压"），方便快速查找
    /// </summary>
    public Dictionary<string, RecipeParam> Parameters { get; set; } = new();
}

/// <summary>
/// 单个配方参数
/// 
/// 对应云端 parametersJsonb 数组中的一个元素：
/// {"id":"xxx", "name":"电压", "min":2.3, "max":3.7, "unit":"伏特"}
/// 
/// 任务层用法：
///   var param = recipeService.GetParam("电压");
///   if (voltage > param.Max) // 超上限报警
/// </summary>
public class RecipeParam
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 本地应急修改时用的自定义值
    /// </summary>
    public string? CustomValue { get; set; }

    public override string ToString()
        => $"{Name}: [{Min} ~ {Max}] {Unit}";
}