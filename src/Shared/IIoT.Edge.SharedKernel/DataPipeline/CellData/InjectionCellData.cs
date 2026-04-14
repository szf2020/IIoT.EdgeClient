namespace IIoT.Edge.SharedKernel.DataPipeline.CellData;

/// <summary>
/// 注液工序电芯生产数据。
/// 在通用电芯数据基础上扩展注液工序专属参数。
/// </summary>
public class InjectionCellData : CellDataBase
{
    public override string ProcessType => "Injection";

    // 业务字段。

    /// <summary>
    /// 工单号，对应本地参数配置。
    /// </summary>
    public string WorkOrderNo { get; set; } = string.Empty;

    // 扫码信息。

    /// <summary>
    /// 电芯条码。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 扫码时间。
    /// </summary>
    public DateTime? ScanTime { get; set; }

    // 注液前数据。

    /// <summary>
    /// 注液前重量，单位为 g。
    /// </summary>
    public double PreInjectionWeight { get; set; }

    // 注液后数据。

    /// <summary>
    /// 注液后重量，单位为 g。
    /// </summary>
    public double PostInjectionWeight { get; set; }

    // 注液量数据。

    /// <summary>
    /// 注液量，单位为 ml。
    /// </summary>
    public double InjectionVolume { get; set; }

    public override string DisplayLabel => string.IsNullOrEmpty(Barcode) ? ProcessType : Barcode;
}
