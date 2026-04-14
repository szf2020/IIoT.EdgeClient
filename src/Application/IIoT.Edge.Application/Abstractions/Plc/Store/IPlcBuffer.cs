namespace IIoT.Edge.Application.Abstractions.Plc.Store;

/// <summary>
/// PLC 缓冲区抽象。
/// 提供基于索引的原始读写能力。
/// </summary>
public interface IPlcBuffer
{
    ushort GetReadValue(int index);
    void SetWriteValue(int index, ushort value);
}

/// <summary>
/// PLC 缓冲区传输抽象。
/// 在基础缓冲区能力上补充批量读写传输支持，供信号交互任务使用。
/// </summary>
public interface IPlcBufferTransport : IPlcBuffer
{
    void UpdateReadBuffer(ushort[] data);
    ushort[] GetWriteBuffer();
}
