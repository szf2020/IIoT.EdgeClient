using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.TestSimulator.Fakes;

public sealed class FakeCapacitySyncTask : ICapacitySyncTask
{
    private readonly ICloudHttpClient _httpClient;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService _logger;

    public FakeCapacitySyncTask(
        ICloudHttpClient cloudHttp,
        ICapacityBufferStore bufferStore,
        ILogService logger)
    {
        _httpClient = cloudHttp;
        _bufferStore = bufferStore;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;

    public async Task<bool> RetryBufferAsync()
    {
        var summaries = await _bufferStore.GetHourlySummaryAsync();
        if (summaries.Count == 0)
        {
            _logger.Info("[FakeCapacitySync] 产能缓冲为空，无需补传");
            return true;
        }

        foreach (var s in summaries)
        {
            var endMinute = s.MinuteBucket == 30 ? 0 : 30;
            var endHour = s.MinuteBucket == 30 ? (s.Hour + 1) % 24 : s.Hour;
            var payload = new
            {
                date = s.Date,
                hour = s.Hour,
                minute = s.MinuteBucket,
                timeLabel = $"{s.Hour:D2}:{s.MinuteBucket:D2}-{endHour:D2}:{endMinute:D2}",
                shiftCode = s.ShiftCode,
                totalCount = s.Total,
                okCount = s.OkCount,
                ngCount = s.NgCount
            };

            await _httpClient.PostAsync("/api/v1/Capacity/hourly", payload);
        }

        await _bufferStore.ClearAllAsync();
        _logger.Info($"[FakeCapacitySync] 补传完成，共 {summaries.Count} 条半小时汇总，缓冲已清空");
        return true;
    }
}
