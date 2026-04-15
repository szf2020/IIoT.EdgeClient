using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Consumers;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Runtime.DataPipeline.Tasks;
using IIoT.Edge.SharedKernel.DataPipeline;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;
using System.Text.Json;

namespace IIoT.Edge.NonUiRegressionTests;

public sealed class RetryTaskCloudMesBehaviorTests
{
    [Fact]
    public async Task CloudBatchRetry_WhenBatchSucceeds_ShouldDeleteBatchRecordsAndContinueOthers()
    {
        CellDataTypeRegistry.Register<InjectionCellData>("Injection");

        var logger = new FakeLogService();
        var failedStore = new FakeFailedRecordStore();
        var deviceService = CreateOnlineDeviceService();
        var cloudBatch = new FakeCloudBatchConsumer();
        cloudBatch.EnqueueResult(true);

        failedStore.PendingRecords.Add(CreateFailedRecord(1, "Cloud", "Cloud", retryCount: 0));
        failedStore.PendingRecords.Add(CreateFailedRecord(2, "Cloud", "Cloud", retryCount: 1));
        failedStore.PendingRecords.Add(CreateFailedRecord(3, "Cloud", "CloudFallback", retryCount: 0));

        var cloudConsumer = new FakeCellDataConsumer("Cloud", 10, "Cloud", result: true);
        var cloudFallbackConsumer = new FakeCellDataConsumer("CloudFallback", 20, "Cloud", result: true);

        var task = new TestableRetryTask(
            channel: "Cloud",
            logger,
            failedStore,
            deviceService,
            consumers: [cloudConsumer, cloudFallbackConsumer],
            cloudBatchConsumer: cloudBatch);

        await task.ExecuteOnceAsync();

        Assert.Equal(1, cloudBatch.ProcessBatchCallCount);
        Assert.Single(cloudBatch.ReceivedBatches);
        Assert.Equal(2, cloudBatch.ReceivedBatches[0].Count);

        Assert.Equal(0, cloudConsumer.ProcessCallCount);
        Assert.Equal(1, cloudFallbackConsumer.ProcessCallCount);

        Assert.Contains(1L, failedStore.DeletedIds);
        Assert.Contains(2L, failedStore.DeletedIds);
        Assert.Contains(3L, failedStore.DeletedIds);
        Assert.Empty(failedStore.Updates);
    }

    [Fact]
    public async Task CloudBatchRetry_WhenBatchFails_ShouldBackoffBatchRecordsAndKeepThem()
    {
        CellDataTypeRegistry.Register<InjectionCellData>("Injection");

        var logger = new FakeLogService();
        var failedStore = new FakeFailedRecordStore();
        var deviceService = CreateOnlineDeviceService();
        var cloudBatch = new FakeCloudBatchConsumer();
        cloudBatch.EnqueueResult(false);

        failedStore.PendingRecords.Add(CreateFailedRecord(10, "Cloud", "Cloud", retryCount: 0));
        failedStore.PendingRecords.Add(CreateFailedRecord(11, "Cloud", "Cloud", retryCount: 2));
        failedStore.PendingRecords.Add(CreateFailedRecord(12, "Cloud", "CloudFallback", retryCount: 0));

        var cloudFallbackConsumer = new FakeCellDataConsumer("CloudFallback", 20, "Cloud", result: true);

        var task = new TestableRetryTask(
            channel: "Cloud",
            logger,
            failedStore,
            deviceService,
            consumers: [new FakeCellDataConsumer("Cloud", 10, "Cloud", result: true), cloudFallbackConsumer],
            cloudBatchConsumer: cloudBatch);

        var before = DateTime.Now;
        await task.ExecuteOnceAsync();

        Assert.Equal(1, cloudBatch.ProcessBatchCallCount);
        Assert.Equal(1, cloudFallbackConsumer.ProcessCallCount);

        Assert.DoesNotContain(10L, failedStore.DeletedIds);
        Assert.DoesNotContain(11L, failedStore.DeletedIds);
        Assert.Contains(12L, failedStore.DeletedIds);

        Assert.True(failedStore.Updates.TryGetValue(10, out var update10));
        Assert.True(failedStore.Updates.TryGetValue(11, out var update11));
        Assert.Equal(1, update10!.RetryCount);
        Assert.Equal(3, update11!.RetryCount);
        Assert.Equal("Cloud batch retry failed.", update10.ErrorMessage);
        Assert.Equal("Cloud batch retry failed.", update11.ErrorMessage);

        var delay10 = (update10.NextRetryTime - before).TotalSeconds;
        var delay11 = (update11.NextRetryTime - before).TotalSeconds;
        Assert.InRange(delay10, 20, 40);
        Assert.InRange(delay11, 20, 40);
    }

    [Fact]
    public async Task MesChannel_ShouldOnlyRunMesConsumersFromFailedTargetOnward()
    {
        CellDataTypeRegistry.Register<InjectionCellData>("Injection");

        var failedStore = new FakeFailedRecordStore();
        failedStore.PendingRecords.Add(CreateFailedRecord(21, "MES", "MES-Primary", retryCount: 0));

        var cloudConsumer = new FakeCellDataConsumer("Cloud", 10, "Cloud", result: true);
        var mesPrimary = new FakeCellDataConsumer("MES-Primary", 20, "MES", result: true);
        var mesSecondary = new FakeCellDataConsumer("MES-Secondary", 30, "MES", result: true);

        var task = new TestableRetryTask(
            channel: "MES",
            new FakeLogService(),
            failedStore,
            CreateOnlineDeviceService(),
            consumers: [cloudConsumer, mesPrimary, mesSecondary]);

        await task.ExecuteOnceAsync();

        Assert.Equal(0, cloudConsumer.ProcessCallCount);
        Assert.Equal(1, mesPrimary.ProcessCallCount);
        Assert.Equal(1, mesSecondary.ProcessCallCount);
        Assert.Contains(21L, failedStore.DeletedIds);
        Assert.Empty(failedStore.Updates);
    }

    [Fact]
    public async Task MesChannel_WhenFailedTargetMissing_ShouldDeleteRecord()
    {
        CellDataTypeRegistry.Register<InjectionCellData>("Injection");

        var failedStore = new FakeFailedRecordStore();
        failedStore.PendingRecords.Add(CreateFailedRecord(22, "MES", "UnknownConsumer", retryCount: 0));

        var mesPrimary = new FakeCellDataConsumer("MES-Primary", 20, "MES", result: true);

        var task = new TestableRetryTask(
            channel: "MES",
            new FakeLogService(),
            failedStore,
            CreateOnlineDeviceService(),
            consumers: [mesPrimary]);

        await task.ExecuteOnceAsync();

        Assert.Equal(0, mesPrimary.ProcessCallCount);
        Assert.Contains(22L, failedStore.DeletedIds);
        Assert.Empty(failedStore.Updates);
    }

    [Fact]
    public async Task CloudChannel_ShouldInvokeDeviceLogAndCapacityRetryHooks()
    {
        var failedStore = new FakeFailedRecordStore();
        var logger = new FakeLogService();
        var deviceLogSync = new FakeDeviceLogSyncTask { RetryResult = false };
        var capacitySync = new FakeCapacitySyncTask { RetryResult = true };

        var task = new TestableRetryTask(
            channel: "Cloud",
            logger,
            failedStore,
            CreateOnlineDeviceService(),
            consumers: [new FakeCellDataConsumer("Cloud", 10, "Cloud", result: true)],
            deviceLogSync: deviceLogSync,
            capacitySync: capacitySync);

        await task.ExecuteOnceAsync();

        Assert.Equal(1, deviceLogSync.RetryBufferCallCount);
        Assert.Equal(1, capacitySync.RetryBufferCallCount);
        Assert.Contains(logger.Entries, x => x.Message.Contains("Device log buffer retry did not fully succeed.", StringComparison.Ordinal));
    }

    private static FakeDeviceService CreateOnlineDeviceService()
    {
        var deviceService = new FakeDeviceService();
        deviceService.SetOnline(new DeviceSession
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "PLC-A",
            MacAddress = "00-11-22-33-44-55",
            ProcessId = Guid.NewGuid()
        });
        return deviceService;
    }

    private static FailedCellRecord CreateFailedRecord(long id, string channel, string failedTarget, int retryCount)
    {
        var cellData = new InjectionCellData
        {
            Barcode = $"BC-{id}",
            WorkOrderNo = $"WO-{id}"
        };

        return new FailedCellRecord
        {
            Id = id,
            Channel = channel,
            ProcessType = "Injection",
            CellDataJson = SerializeCellData(cellData),
            FailedTarget = failedTarget,
            ErrorMessage = "seed",
            RetryCount = retryCount,
            NextRetryTime = DateTime.Now.AddMinutes(-1),
            CreatedAt = DateTime.Now.AddMinutes(-2)
        };
    }

    private static string SerializeCellData(CellDataBase cellData)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(cellData, cellData.GetType(), jsonOptions);
    }

    private sealed class TestableRetryTask(
        string channel,
        FakeLogService logger,
        FakeFailedRecordStore failedStore,
        FakeDeviceService deviceService,
        IEnumerable<ICellDataConsumer> consumers,
        IDeviceLogSyncTask? deviceLogSync = null,
        ICapacitySyncTask? capacitySync = null,
        ICloudBatchConsumer? cloudBatchConsumer = null)
        : RetryTask(channel, logger, failedStore, deviceService, consumers, deviceLogSync, capacitySync, cloudBatchConsumer)
    {
        public Task ExecuteOnceAsync() => base.ExecuteAsync();
    }
}
