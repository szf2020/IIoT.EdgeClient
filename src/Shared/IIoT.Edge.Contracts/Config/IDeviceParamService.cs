using IIoT.Edge.Common.Enums;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Contracts.Config;

public interface IDeviceParamService
{
    Task<List<DeviceParamEntity>> GetByDeviceAsync(int deviceId);
    Task<string?> GetValueAsync(int deviceId, DeviceParamKey key);
    Task SaveAsync(int deviceId, List<DeviceParamEntity> paramList);
    Task DeleteAsync(int deviceId, int paramId);
}