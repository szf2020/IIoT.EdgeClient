namespace IIoT.Edge.DataMapping.Cloud.Injection;

/// <summary>
/// 注液过站数据云端请求体
/// 
/// 对应 POST /api/v1/PassStation/injection
/// 字段名与 Swagger ReceiveInjectionPassCommand 一一对应
/// </summary>
public class InjectionCloudDto
{
    public Guid DeviceId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string CellResult { get; set; } = string.Empty;
    public DateTime? CompletedTime { get; set; }
    public double PreInjectionWeight { get; set; }
    public double PostInjectionWeight { get; set; }
    public double InjectionVolume { get; set; }
}