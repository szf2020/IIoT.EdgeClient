using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.TestSimulator.Fakes;
using IIoT.Edge.TestSimulator.Services;
using IIoT.Edge.TestSimulator.Tasks;

namespace IIoT.Edge.TestSimulator.Scenarios;

/// <summary>
/// 场景三：恢复补传
/// 
/// 前置：切回 Online，场景二的 SQLite 数据保留
/// 执行：重置 NextRetryTime → 手动触发 TestRetryTask.TriggerAsync()
/// 断言：FailedStore(Cloud)=0 / CapacityBuffer=0 / HTTP 调用 ≥ 5
/// </summary>
public sealed class RetryScenario : ITestScenario
{
    public string Name => "场景三：恢复补传";

    private readonly FakeHttpClient      _httpClient;
    private readonly FakeDeviceService   _deviceService;
    private readonly TestRetryTask       _retryTask;
    private readonly SimDataHelper       _dataHelper;
    private readonly IFailedRecordStore  _failedStore;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService         _logger;

    public RetryScenario(
        FakeHttpClient       httpClient,
        FakeDeviceService    deviceService,
        TestRetryTask        retryTask,
        SimDataHelper        dataHelper,
        IFailedRecordStore   failedStore,
        ICapacityBufferStore bufferStore,
        ILogService          logger)
    {
        _httpClient    = httpClient;
        _deviceService = deviceService;
        _retryTask     = retryTask;
        _dataHelper    = dataHelper;
        _failedStore   = failedStore;
        _bufferStore   = bufferStore;
        _logger        = logger;
    }

    public async Task<ScenarioResult> RunAsync(CancellationToken ct = default)
    {
        var assertions = new List<AssertionResult>();

        try
        {
            // ── 前置：切回在线，重置 HTTP 计数 ─────────────────
            _deviceService.CurrentState = NetworkState.Online;
            _httpClient.IsOnline        = true;
            _httpClient.Reset();

            // 解除 30 秒重传冷却，让记录立即可被 RetryTask 捞到
            await _dataHelper.ResetRetryTimesAsync();

            // ── 执行：手动触发一轮重传（不等 5 秒定时器） ──────
            await _retryTask.TriggerAsync();
            await Task.Delay(300, ct); // 等待所有 Store 操作完成

            // ── 断言 ───────────────────────────────────────────
            var failedCount = await _failedStore.GetCountAsync("Cloud");
            var bufferCount = await _bufferStore.GetCountAsync();
            var callCount   = _httpClient.CallCount;

            assertions.Add(Assert("FailedRecordStore(Cloud) == 0", failedCount == 0,    "0",   failedCount.ToString()));
            assertions.Add(Assert("CapacityBufferStore == 0",       bufferCount == 0,    "0",   bufferCount.ToString()));
            assertions.Add(Assert("FakeHttpClient.CallCount >= 5",  callCount   >= 5, ">=5",  callCount.ToString()));
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Name}] 异常: {ex.Message}");
            return new ScenarioResult { Name = Name, Passed = false, Error = ex.Message, Assertions = assertions };
        }

        return new ScenarioResult
        {
            Name       = Name,
            Passed     = assertions.All(a => a.Passed),
            Assertions = assertions
        };
    }

    private static AssertionResult Assert(string desc, bool passed, string expected, string actual)
        => new() { Description = desc, Passed = passed, Expected = expected, Actual = actual };
}
