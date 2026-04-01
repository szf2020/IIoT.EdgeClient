using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.TestSimulator.Fakes;

/// <summary>
/// 替换真实 HTTP 客户端，记录调用历史，外部控制成功/失败
/// </summary>
public sealed class FakeHttpClient : ICloudHttpClient
{
    private int _callCount;
    private readonly List<string> _urlHistory = new();
    private readonly object _lock = new();

    /// <summary>控制 PostAsync 返回 true/false</summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>总调用次数（线程安全）</summary>
    public int CallCount => _callCount;

    /// <summary>记录每次调用的 URL</summary>
    public IReadOnlyList<string> UrlHistory
    {
        get { lock (_lock) return _urlHistory.ToList(); }
    }

    /// <summary>清空历史，供场景切换时调用</summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _callCount, 0);
        lock (_lock) _urlHistory.Clear();
    }

    public Task<bool> PostAsync(string url, object payload)
    {
        Interlocked.Increment(ref _callCount);
        lock (_lock) _urlHistory.Add(url);
        return Task.FromResult(IsOnline);
    }

    public Task<string?> PostWithResponseAsync(string url, object payload)
    {
        Interlocked.Increment(ref _callCount);
        lock (_lock) _urlHistory.Add(url);
        return Task.FromResult(IsOnline ? "{\"success\":true}" : (string?)null);
    }

    public Task<string?> GetAsync(string url)
    {
        Interlocked.Increment(ref _callCount);
        lock (_lock) _urlHistory.Add(url);
        return Task.FromResult(IsOnline ? "{}" : (string?)null);
    }
}
