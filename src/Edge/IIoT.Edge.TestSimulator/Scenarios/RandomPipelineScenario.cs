using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Common.DataPipeline.DeviceLog;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.TestSimulator.Fakes;
using IIoT.Edge.TestSimulator.Services;
using IIoT.Edge.TestSimulator.Tasks;
using IIoT.Edge.Tasks.DataPipeline.Services;

namespace IIoT.Edge.TestSimulator.Scenarios;

/// <summary>
/// 场景四：随机数据压力补传
/// 
/// 1) 离线随机产生：生产记录 + 日志记录（写入 SQLite）
/// 2) 切回在线触发 RetryTask，一次性验证云端补传链路
/// </summary>
public sealed class RandomPipelineScenario : ITestScenario
{
    public string Name => "场景四：随机数据压力补传";

    private readonly FakeHttpClient _httpClient;
    private readonly FakeDeviceService _deviceService;
    private readonly DataPipelineService _pipeline;
    private readonly TestRetryTask _retryTask;
    private readonly SimDataHelper _dataHelper;
    private readonly IFailedRecordStore _failedStore;
    private readonly ICapacityBufferStore _capacityBufferStore;
    private readonly IDeviceLogBufferStore _deviceLogBufferStore;
    private readonly ILogService _logger;

    public RandomPipelineScenario(
        FakeHttpClient httpClient,
        FakeDeviceService deviceService,
        DataPipelineService pipeline,
        TestRetryTask retryTask,
        SimDataHelper dataHelper,
        IFailedRecordStore failedStore,
        ICapacityBufferStore capacityBufferStore,
        IDeviceLogBufferStore deviceLogBufferStore,
        ILogService logger)
    {
        _httpClient = httpClient;
        _deviceService = deviceService;
        _pipeline = pipeline;
        _retryTask = retryTask;
        _dataHelper = dataHelper;
        _failedStore = failedStore;
        _capacityBufferStore = capacityBufferStore;
        _deviceLogBufferStore = deviceLogBufferStore;
        _logger = logger;
    }

    public async Task<ScenarioResult> RunAsync(CancellationToken ct = default)
    {
        var assertions = new List<AssertionResult>();

        const int productionCount = 120;
        const int logCount = 60;

        try
        {
            // ── 阶段A：离线写 SQLite ───────────────────────────
            _deviceService.SetOffline();
            _httpClient.IsOnline = false;
            _httpClient.Reset();

            var random = new Random();
            for (int i = 0; i < productionCount; i++)
                _pipeline.Enqueue(BuildRandomRecord(i + 1, random));

            await _deviceLogBufferStore.SaveBatchAsync(BuildRandomLogs(logCount, random));

            await WaitQueueEmptyAsync(ct);
            await Task.Delay(400, ct);

            var failedBefore = await _failedStore.GetCountAsync("Cloud");
            var capacityBefore = await _capacityBufferStore.GetCountAsync();
            var logsBefore = await _deviceLogBufferStore.GetCountAsync();

            assertions.Add(Assert("离线云重传队列 > 0", failedBefore > 0, ">0", failedBefore.ToString()));
            assertions.Add(Assert("离线产能缓冲 > 0", capacityBefore > 0, ">0", capacityBefore.ToString()));
            assertions.Add(Assert("离线日志缓冲 == 60", logsBefore == logCount, logCount.ToString(), logsBefore.ToString()));

            // ── 阶段B：在线触发补传 ───────────────────────────
            _deviceService.SetOnline();
            _httpClient.IsOnline = true;
            _httpClient.Reset();

            await _dataHelper.ResetRetryTimesAsync();
            await _retryTask.TriggerAsync();
            await Task.Delay(500, ct);

            var failedAfter = await _failedStore.GetCountAsync("Cloud");
            var capacityAfter = await _capacityBufferStore.GetCountAsync();
            var logsAfter = await _deviceLogBufferStore.GetCountAsync();
            var urls = _httpClient.UrlHistory;

            assertions.Add(Assert("补传后云重传队列 == 0", failedAfter == 0, "0", failedAfter.ToString()));
            assertions.Add(Assert("补传后产能缓冲 == 0", capacityAfter == 0, "0", capacityAfter.ToString()));
            assertions.Add(Assert("补传后日志缓冲 == 0", logsAfter == 0, "0", logsAfter.ToString()));
            assertions.Add(Assert("命中生产上传接口", urls.Any(u => u.Contains("/api/test/passstation")), "true", "false"));
            assertions.Add(Assert("命中产能上传接口", urls.Any(u => u.Contains("/api/v1/Capacity/hourly")), "true", "false"));
            assertions.Add(Assert("命中日志上传接口", urls.Any(u => u.Contains("/api/v1/DeviceLog")), "true", "false"));
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Name}] 异常: {ex.Message}");
            return new ScenarioResult { Name = Name, Passed = false, Error = ex.Message, Assertions = assertions };
        }

        return new ScenarioResult
        {
            Name = Name,
            Passed = assertions.All(a => a.Passed),
            Assertions = assertions
        };
    }

    private async Task WaitQueueEmptyAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (_pipeline.PendingCount > 0 && DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            await Task.Delay(80, ct);
    }

    private static CellCompletedRecord BuildRandomRecord(int index, Random random)
    {
        var baseDate = DateTime.Today;
        var totalSeconds = random.Next(0, 24 * 60 * 60);
        var completed = baseDate.AddSeconds(totalSeconds);

        var pre = 24 + random.NextDouble() * 3;
        var delta = random.NextDouble() * 4;
        var ok = random.Next(0, 100) >= 8;

        return new CellCompletedRecord
        {
            CellData = new InjectionCellData
            {
                DeviceName = "TestDevice",
                DeviceCode = "SIM-001",
                Barcode = $"RND-{index:D4}",
                CellResult = ok,
                CompletedTime = completed,
                ScanTime = completed.AddSeconds(-random.Next(1, 20)),
                PreInjectionWeight = pre,
                PostInjectionWeight = pre + delta,
                InjectionVolume = delta
            }
        };
    }

    private static IEnumerable<DeviceLogRecord> BuildRandomLogs(int count, Random random)
    {
        var levels = new[] { "INFO", "WARN", "ERROR" };
        for (int i = 0; i < count; i++)
        {
            var ts = DateTime.Now.AddSeconds(-random.Next(1, 3600));
            yield return new DeviceLogRecord
            {
                Level = levels[random.Next(0, levels.Length)],
                Message = $"[RANDOM] Log #{i + 1} value={random.Next(1000, 9999)}",
                LogTime = ts.ToString("O"),
                CreatedAt = DateTime.Now.ToString("O")
            };
        }
    }

    private static AssertionResult Assert(string desc, bool passed, string expected, string actual)
        => new() { Description = desc, Passed = passed, Expected = expected, Actual = actual };
}
