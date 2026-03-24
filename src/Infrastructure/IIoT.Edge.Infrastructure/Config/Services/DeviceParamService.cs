using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Contracts.Config;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Infrastructure.Config.Services;

public class DeviceParamService : IDeviceParamService
{
    private readonly IRepository<DeviceParamEntity> _repo;
    private readonly IEdgeCacheService _cache;
    private const string CachePrefix = "Config:DeviceParam:";

    public DeviceParamService(
        IRepository<DeviceParamEntity> repo,
        IEdgeCacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<List<DeviceParamEntity>> GetByDeviceAsync(
        int deviceId)
    {
        var cacheKey = CachePrefix + deviceId;
        var cached = _cache
            .Get<List<DeviceParamEntity>>(cacheKey);
        if (cached != null) return cached;

        var list = await _repo.GetListAsync(
            x => x.NetworkDeviceId == deviceId,
            CancellationToken.None);
        _cache.Set(cacheKey, list);
        return list;
    }

    public async Task<string?> GetValueAsync(
        int deviceId, DeviceParamKey key)
    {
        var all = await GetByDeviceAsync(deviceId);
        return all.FirstOrDefault(
            x => x.Name == key.ToString())?.Value;
    }

    public async Task SaveAsync(int deviceId,
        List<DeviceParamEntity> paramList)
    {
        // 1. 过滤空行 + 按Name去重
        var valid = paramList
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .Select(g => g.Last())
            .ToList();

        // 2. 批量删除该设备旧数据
        await _repo.ExecuteDeleteAsync(
            x => x.NetworkDeviceId == deviceId);

        // 3. 全量写入
        for (int i = 0; i < valid.Count; i++)
        {
            var p = valid[i];
            _repo.Add(new DeviceParamEntity(
                deviceId, p.Name, p.Value, p.Unit)
            {
                MinValue = p.MinValue,
                MaxValue = p.MaxValue,
                SortOrder = i + 1
            });
        }
        await _repo.SaveChangesAsync();

        // 4. 清缓存
        _cache.Remove(CachePrefix + deviceId);
    }

    public async Task DeleteAsync(int deviceId, int paramId)
    {
        await _repo.ExecuteDeleteAsync(x => x.Id == paramId);
        _cache.Remove(CachePrefix + deviceId);
    }
}