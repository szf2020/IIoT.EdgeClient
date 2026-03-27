namespace IIoT.Edge.Common.DataPipeline.CellData;

/// <summary>
/// 注液机电芯生产数据
/// 
/// 继承 CellDataBase，扩展注液工序的专属参数
/// 
/// 调试时断点展开即可看到所有字段：
///   Barcode = "CELL20260327001"
///   ScanTime = 2026-03-27 10:28:00
///   PreInjectionWeight = 25.5
///   PostInjectionWeight = 28.3
///   InjectionVolume = 2.8
///   CellResult = true
/// </summary>
public class InjectionCellData : CellDataBase
{
    public override string ProcessType => "Injection";

    // ── 业务字段 ─────────────────────────────────────────────

    /// <summary>
    /// 工单号（本地参数配置）
    /// </summary>
    public string WorkOrderNo { get; set; } = string.Empty;

    // ── 扫码 ─────────────────────────────────────────────────

    /// <summary>
    /// 电芯条码
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 扫码时间
    /// </summary>
    public DateTime? ScanTime { get; set; }

    // ── 注液前 ───────────────────────────────────────────────

    /// <summary>
    /// 注液前重量（g）
    /// </summary>
    public double PreInjectionWeight { get; set; }

    // ── 注液后 ───────────────────────────────────────────────

    /// <summary>
    /// 注液后重量（g）
    /// </summary>
    public double PostInjectionWeight { get; set; }

    // ── 注液量 ───────────────────────────────────────────────

    /// <summary>
    /// 注液量（ml）
    /// </summary>
    public double InjectionVolume { get; set; }

    public override string DisplayLabel => string.IsNullOrEmpty(Barcode) ? ProcessType : Barcode;
}