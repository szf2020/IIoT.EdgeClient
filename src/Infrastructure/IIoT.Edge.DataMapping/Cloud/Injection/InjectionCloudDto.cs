using System;

namespace IIoT.Edge.DataMapping.Cloud.Injection;

/// <summary>
/// 注液过站数据云端请求项
/// 
/// 对应 POST /api/v1/PassStation/injection/batch 的 items[]
/// </summary>
public class InjectionCloudDto
{
    public string Barcode { get; set; } = string.Empty;
    public string CellResult { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }
    public DateTime PreInjectionTime { get; set; }
    public double PreInjectionWeight { get; set; }
    public DateTime PostInjectionTime { get; set; }
    public double PostInjectionWeight { get; set; }
    public double InjectionVolume { get; set; }
}