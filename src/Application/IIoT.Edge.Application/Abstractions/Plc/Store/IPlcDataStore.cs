namespace IIoT.Edge.Application.Abstractions.Plc.Store;

/// <summary>
/// PLC 数据缓冲存储契约。
/// 负责按设备注册并获取运行期缓冲区。
/// </summary>
public interface IPlcDataStore
{
    void Register(int networkDeviceId, int readSize, int writeSize);

    /// <summary>
    /// 获取运行期信号交互使用的传输缓冲区。
    /// </summary>
    IPlcBufferTransport? GetBuffer(int networkDeviceId);

    bool HasDevice(int networkDeviceId);
}
