using IIoT.Edge.Common.Domain;

namespace IIoT.Edge.Domain.Config.Aggregates;

public class SystemConfigEntity : BaseEntity<int>, IAggregateRoot
{
    protected SystemConfigEntity() { }

    public SystemConfigEntity(
        string key, string value, string? description = null)
    {
        Key = key;
        Value = value;
        Description = description;
    }

    /// <summary>参数键名，唯一（如 "Mes.Address"）</summary>
    public string Key { get; set; } = null!;

    /// <summary>参数值（统一存字符串）</summary>
    public string Value { get; set; } = null!;

    /// <summary>说明</summary>
    public string? Description { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }
}