namespace IIoT.Edge.Contracts.Cache;

/// <summary>
/// 通用内存缓存服务。
/// 读：先查缓存，未命中返回 null / default。
/// 写/改/删：操作完数据库后主动清缓存。
/// </summary>
public interface IEdgeCacheService
{
    /// <summary>获取缓存，未命中返回 default</summary>
    T? Get<T>(string key);

    /// <summary>写入缓存</summary>
    void Set<T>(string key, T value);

    /// <summary>删除单个缓存</summary>
    void Remove(string key);

    /// <summary>按前缀批量删除（如 "DeviceParam:10"）</summary>
    void RemoveByPrefix(string prefix);

    /// <summary>清空所有缓存</summary>
    void Clear();

    /// <summary>判断缓存是否存在</summary>
    bool Contains(string key);
}