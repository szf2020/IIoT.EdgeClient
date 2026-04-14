namespace IIoT.Edge.Application.Abstractions.Device;

/// <summary>
/// 云端 API 路径提供器契约。
/// 为上层提供产能查询相关接口路径。
/// </summary>
public interface ICloudApiPathProvider
{
    string GetCapacityHourlyPath();
    string GetCapacitySummaryPath();
    string GetCapacitySummaryRangePath();
}
