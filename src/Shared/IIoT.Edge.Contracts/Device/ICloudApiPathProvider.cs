namespace IIoT.Edge.Contracts.Device;

/// <summary>
/// Provides configurable cloud API paths used by upper layers.
/// </summary>
public interface ICloudApiPathProvider
{
    string GetCapacityHourlyPath();
    string GetCapacitySummaryPath();
    string GetCapacitySummaryRangePath();
}
