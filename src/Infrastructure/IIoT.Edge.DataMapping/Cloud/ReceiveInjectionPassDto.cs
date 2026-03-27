namespace IIoT.Edge.DataMapping.Cloud;

/// <summary>
/// 云端过站数据接口请求体
/// 
/// 对应 POST /api/v1/PassStation/injection
/// 字段名与 Swagger ReceiveInjectionPassCommand 一一对应
/// </summary>
public sealed class ReceiveInjectionPassDto
{
    public Guid DeviceId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string CellResult { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }
    public DateTime PreInjectionTime { get; set; }
    public double PreInjectionWeight { get; set; }
    public DateTime PostInjectionTime { get; set; }
    public double PostInjectionWeight { get; set; }
    public double InjectionVolume { get; set; }
}