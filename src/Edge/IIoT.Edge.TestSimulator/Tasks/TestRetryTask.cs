using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Tasks.DataPipeline.Tasks;

namespace IIoT.Edge.TestSimulator.Tasks;

/// <summary>
/// 继承 RetryTask，对外暴露 TriggerAsync() 供场景立即触发一轮重传
/// 不等 5 秒定时器，测试直接调用即可
/// </summary>
public sealed class TestRetryTask : RetryTask
{
    public TestRetryTask(
        ILogService                 logger,
        IFailedRecordStore          failedStore,
        IDeviceService              deviceService,
        IEnumerable<ICellDataConsumer> consumers,
        IDeviceLogSyncTask?         deviceLogSync = null,
        ICapacitySyncTask?          capacitySync  = null,
        ICloudBatchConsumer?        cloudBatchConsumer = null)
        : base("Cloud", logger, failedStore, deviceService, consumers, deviceLogSync, capacitySync, cloudBatchConsumer)
    {
    }

    /// <summary>立即执行一轮重传逻辑（绕过 5 秒定时间隔）</summary>
    public Task TriggerAsync() => ExecuteAsync();
}
