namespace IIoT.Edge.Common.DataPipeline.CellData;

/// <summary>
/// 电芯生产数据基类
/// 
/// 所有设备类型共有的字段
/// 子类按设备类型扩展各自的生产参数和业务字段
/// 
/// 数据填充时机：
///   PlcDeviceId / DeviceName     ← 任务创建 CellData 时从 ProductionContext 填入
///   DeviceCode / ProcessName / ShiftCode ← 流程启动时从本地参数配置读取
///   CellResult / CompletedTime           ← 组装确认判定后填入
/// </summary>
public abstract class CellDataBase
{
    /// <summary>
    /// PLC 设备数据库主键（NetworkDeviceEntity.Id）
    /// </summary>
    public int PlcDeviceId { get; set; }

    /// <summary>
    /// 本地设备名（"注液机1"），全局聚合 key
    /// 用于关联 ProductionContext、产能统计、消费链定位
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// MES 机台编码（本地参数配置里设置，后期由 MES 动态下发）
    /// </summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 工序名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 班次编码（"白班" / "夜班"）
    /// </summary>
    public string ShiftCode { get; set; } = string.Empty;

    /// <summary>
    /// 电芯综合结果（true=OK, false=NG, null=未判定）
    /// </summary>
    public bool? CellResult { get; set; }

    /// <summary>
    /// 完成时间（组装确认后填入）
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 工序类型标识，子类覆写
    /// 用于 JSON 多态序列化和消费者识别数据类型
    /// 例如："Injection"、"DieCutting"、"PreCharge"
    /// </summary>
    public abstract string ProcessType { get; }

    /// <summary>
    /// 显示标识（日志/调试用）
    /// 默认返回工序类型，有条码的子类覆写返回条码
    /// </summary>
    public virtual string DisplayLabel => ProcessType;
}