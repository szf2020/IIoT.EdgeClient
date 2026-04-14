using IIoT.Edge.Application.Abstractions.Device;

namespace IIoT.Edge.Infrastructure.Integration.Device.Cache;

public class DeviceSessionFileCacheStore
{
    private readonly string _cacheFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "device_cache.json");

    public void Save(DeviceSession session)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(session);
        File.WriteAllText(_cacheFilePath, json);
    }

    public DeviceSession? TryLoad(string instanceId)
    {
        if (!File.Exists(_cacheFilePath))
        {
            return null;
        }

        var json = File.ReadAllText(_cacheFilePath);
        var session = System.Text.Json.JsonSerializer.Deserialize<DeviceSession>(json);
        return session is null
            ? null
            : session with { MacAddress = instanceId };
    }
}
