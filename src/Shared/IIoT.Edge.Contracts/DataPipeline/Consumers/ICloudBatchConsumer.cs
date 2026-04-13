using IIoT.Edge.Common.DataPipeline;

namespace IIoT.Edge.Contracts.DataPipeline.Consumers;

/// <summary>
/// 云端批量上报能力（用于离线补传批次上传）
/// </summary>
public interface ICloudBatchConsumer
{
    Task<bool> ProcessBatchAsync(IReadOnlyList<CellCompletedRecord> records);
}
