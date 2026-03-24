using IIoT.Edge.Common.Domain;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Domain.Config.Aggregates;

public class DeviceParamEntity : BaseEntity<int>, IAggregateRoot
{
    protected DeviceParamEntity() { }

    public DeviceParamEntity(
        int networkDeviceId,
        string name,
        string value,
        string? unit = null)
    {
        NetworkDeviceId = networkDeviceId;
        Name = name;
        Value = value;
        Unit = unit;
    }

    /// <summary>外键，关联 NetworkDeviceEntity</summary>
    public int NetworkDeviceId { get; set; }

    /// <summary>参数名（如"切刀速度"）</summary>
    public string Name { get; set; } = null!;

    /// <summary>当前值</summary>
    public string Value { get; set; } = null!;

    /// <summary>单位</summary>
    public string? Unit { get; set; }

    /// <summary>下限</summary>
    public string? MinValue { get; set; }

    /// <summary>上限</summary>
    public string? MaxValue { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    // 导航属性
    public NetworkDeviceEntity NetworkDevice { get; set; } = null!;
}