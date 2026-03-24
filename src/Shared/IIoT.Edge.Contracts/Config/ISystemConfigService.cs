using IIoT.Edge.Common.Enums;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Contracts.Config;

public interface ISystemConfigService
{
    Task<List<SystemConfigEntity>> GetAllAsync();
    Task<string?> GetValueAsync(SystemConfigKey key);
    Task SaveAsync(List<SystemConfigEntity> configs);
    Task DeleteAsync(int id);
    Task SeedDefaultsAsync();
}