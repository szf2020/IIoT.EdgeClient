using IIoT.Edge.Application.Abstractions.Cache;
using System.Collections.Concurrent;

namespace IIoT.Edge.Infrastructure.Persistence.EfCore.Caching.Memory;

public class EdgeMemoryCacheService : IEdgeCacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    public void Set<T>(string key, T value)
    {
        if (value is null)
        {
            return;
        }

        _cache[key] = value;
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        var keys = _cache.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public bool Contains(string key)
    {
        return _cache.ContainsKey(key);
    }
}
