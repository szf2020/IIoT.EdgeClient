using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.CloudSync.Config;

public interface ICloudApiEndpointProvider : ICloudApiPathProvider
{
    string BuildUrl(string relativeOrAbsoluteUrl);
    string GetClientCode();
    string GetDeviceInstancePath();
    string GetIdentityDeviceLoginPath();
    string GetPassStationInjectionBatchPath();
    string GetDeviceLogPath();
    string BuildRecipeByDevicePath(Guid deviceId);
}
