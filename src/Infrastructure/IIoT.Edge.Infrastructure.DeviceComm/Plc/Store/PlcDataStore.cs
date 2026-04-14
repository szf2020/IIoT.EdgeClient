using IIoT.Edge.Application.Abstractions.Plc.Store;

namespace IIoT.Edge.Infrastructure.DeviceComm.Plc.Store;

public class PlcDataStore : IPlcDataStore
{
    private readonly Dictionary<int, PlcBuffer> _buffers = new();

    public void Register(int networkDeviceId, int readSize, int writeSize)
    {
        if (!_buffers.ContainsKey(networkDeviceId))
            _buffers[networkDeviceId] = new PlcBuffer(readSize, writeSize);
    }

    public IPlcBufferTransport? GetBuffer(int networkDeviceId)
        => _buffers.TryGetValue(networkDeviceId, out var buffer) ? buffer : null;

    public bool HasDevice(int networkDeviceId)
        => _buffers.ContainsKey(networkDeviceId);
}
