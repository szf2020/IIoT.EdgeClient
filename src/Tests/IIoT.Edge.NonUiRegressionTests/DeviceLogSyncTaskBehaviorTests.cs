using IIoT.Edge.Infrastructure.Integration.DeviceLog;
using IIoT.Edge.SharedKernel.DataPipeline.DeviceLog;

namespace IIoT.Edge.NonUiRegressionTests;

public sealed class DeviceLogSyncTaskBehaviorTests
{
    [Fact]
    public async Task RetryBuffer_WhenOffline_ShouldSkipPostAndReturnFalse()
    {
        var cloudHttp = new FakeCloudHttpClient();
        var endpointProvider = new FakeCloudApiEndpointProvider();
        var deviceService = new FakeDeviceService
        {
            CurrentState = IIoT.Edge.Application.Abstractions.Device.NetworkState.Offline,
            HasDeviceId = false,
            CurrentDevice = null
        };
        var bufferStore = new FakeDeviceLogBufferStore();
        bufferStore.Records.Add(new DeviceLogRecord
        {
            Id = 1,
            Level = "Info",
            Message = "offline-buffer",
            LogTime = DateTime.UtcNow.ToString("O"),
            CreatedAt = DateTime.UtcNow.ToString("O")
        });

        var task = new DeviceLogSyncTask(
            cloudHttp,
            endpointProvider,
            deviceService,
            bufferStore,
            new FakeLogService());

        var result = await task.RetryBufferAsync();

        Assert.False(result);
        Assert.Equal(0, cloudHttp.PostCallCount);
        Assert.Empty(bufferStore.DeletedIds);
    }

    [Fact]
    public async Task RetryBuffer_WhenOnline_ShouldPostAndDeleteBufferedRows()
    {
        var cloudHttp = new FakeCloudHttpClient();
        cloudHttp.EnqueuePostResult(true);

        var endpointProvider = new FakeCloudApiEndpointProvider();
        var deviceService = new FakeDeviceService();
        deviceService.SetOnline(new IIoT.Edge.Application.Abstractions.Device.DeviceSession
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "PLC-A",
            MacAddress = "00-11-22-33-44-55",
            ProcessId = Guid.NewGuid()
        });

        var bufferStore = new FakeDeviceLogBufferStore();
        bufferStore.Records.Add(new DeviceLogRecord
        {
            Id = 1,
            Level = "Warn",
            Message = "buffer-1",
            LogTime = DateTime.UtcNow.ToString("O"),
            CreatedAt = DateTime.UtcNow.ToString("O")
        });

        var task = new DeviceLogSyncTask(
            cloudHttp,
            endpointProvider,
            deviceService,
            bufferStore,
            new FakeLogService());

        var result = await task.RetryBufferAsync();

        Assert.True(result);
        Assert.Equal(1, cloudHttp.PostCallCount);
        Assert.Single(bufferStore.DeletedIds);
        Assert.Equal(1L, bufferStore.DeletedIds[0]);
    }
}
