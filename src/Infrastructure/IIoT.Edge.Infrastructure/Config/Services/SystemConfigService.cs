using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Contracts.Config;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Infrastructure.Config.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly IRepository<SystemConfigEntity> _repo;
    private readonly IEdgeCacheService _cache;
    private const string CacheKey = "Config:SystemAll";

    public SystemConfigService(
        IRepository<SystemConfigEntity> repo,
        IEdgeCacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<List<SystemConfigEntity>> GetAllAsync()
    {
        var cached = _cache.Get<List<SystemConfigEntity>>(CacheKey);
        if (cached != null) return cached;

        var list = await _repo.GetListAsync(
            _ => true, CancellationToken.None);
        _cache.Set(CacheKey, list);
        return list;
    }

    public async Task<string?> GetValueAsync(SystemConfigKey key)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(
            x => x.Key == key.ToString())?.Value;
    }

    public async Task SaveAsync(List<SystemConfigEntity> configs)
    {
        // 1. 过滤空行 + 按Key去重
        var valid = configs
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .Select(g => g.Last())
            .ToList();

        // 2. 批量删除旧数据（独立DbContext，一条SQL）
        await _repo.ExecuteDeleteAsync(_ => true);

        // 3. 全量写入（新的DbContext批次）
        for (int i = 0; i < valid.Count; i++)
        {
            var c = valid[i];
            _repo.Add(new SystemConfigEntity(
                c.Key, c.Value, c.Description)
            { SortOrder = i + 1 });
        }
        await _repo.SaveChangesAsync();

        // 4. 清缓存
        _cache.Remove(CacheKey);
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.ExecuteDeleteAsync(x => x.Id == id);
        _cache.Remove(CacheKey);
    }

    public async Task SeedDefaultsAsync() { }
}