using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.TestSimulator.Fakes;

public sealed class FakeDeviceLogSyncTask : IDeviceLogSyncTask
{
    private readonly ICloudHttpClient _httpClient;
    private readonly IDeviceLogBufferStore _bufferStore;
    private readonly IDeviceService _deviceService;
    private readonly ILogService _logger;

    public FakeDeviceLogSyncTask(
        ICloudHttpClient httpClient,
        IDeviceLogBufferStore bufferStore,
        IDeviceService deviceService,
        ILogService logger)
    {
        _httpClient = httpClient;
        _bufferStore = bufferStore;
        _deviceService = deviceService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;

    public async Task<bool> RetryBufferAsync()
    {
        var device = _deviceService.CurrentDevice;
        if (device is null)
            return false;

        while (true)
        {
            var pending = await _bufferStore.GetPendingAsync(100);
            if (pending.Count == 0)
            {
                _logger.Info("[FakeDeviceLogSync] 日志缓冲为空，无需补传");
                return true;
            }

            var payload = new
            {
                logs = pending.Select(x => new
                {
                    deviceId = device.DeviceId,
                    level = x.Level,
                    message = x.Message,
                    logTime = x.LogTime
                }).ToArray()
            };

            var ok = await _httpClient.PostAsync("/api/v1/DeviceLog", payload);
            if (!ok)
                return false;

            await _bufferStore.DeleteBatchAsync(pending.Select(x => x.Id));
        }
    }
}
