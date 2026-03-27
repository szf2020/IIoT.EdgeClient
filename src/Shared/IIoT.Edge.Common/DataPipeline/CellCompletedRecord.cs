namespace IIoT.Edge.Common.DataPipeline;

/// <summary>
/// 电芯完成记录 — 从 ProductionContext 取出后的数据载体
/// 
/// 包含电芯在设备上流转期间积累的所有数据（以 JSON 形式）
/// 进入内存队列后，由 ProcessQueueTask 按顺序消费
/// </summary>
public class CellCompletedRecord
{
    /// <summary>
    /// 电芯条码（唯一标识）
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 本地设备ID（NetworkDeviceEntity.Id，始终有值）
    /// </summary>
    public int LocalDeviceId { get; set; }

    /// <summary>
    /// 设备名称（日志/Excel用）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 云端设备ID（寻址成功后才有，未寻址时为 null）
    /// 数据上报时携带，对应云端 ReceiveInjectionPassCommand.DeviceId
    /// </summary>
    public Guid? CloudDeviceId { get; set; }

    /// <summary>
    /// 电芯综合结果（true=OK, false=NG）
    /// </summary>
    public bool CellResult { get; set; }

    /// <summary>
    /// 电芯全部数据的 JSON（CellBag 整个序列化）
    /// 不同机台、不同任务写入的 key 都在这里面
    /// </summary>
    public string DataJson { get; set; } = string.Empty;

    /// <summary>
    /// 组装确认（最后一个流程）完成的时间
    /// </summary>
    public DateTime CompletedTime { get; set; }
}