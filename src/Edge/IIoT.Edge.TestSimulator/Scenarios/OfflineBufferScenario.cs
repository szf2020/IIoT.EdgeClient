using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.TestSimulator.Fakes;
using IIoT.Edge.Tasks.DataPipeline.Services;

namespace IIoT.Edge.TestSimulator.Scenarios;

/// <summary>
/// 场景二：离线落库（随机化）
/// 
/// 前置：Offline + HTTP 断线 + 清空上一场景 HTTP 历史
/// 执行：入队随机 N 条离线记录（默认区间 5~12）
/// 断言：FailedStore(Cloud)=N / CapacityBuffer=N / HTTP 调用=0
/// </summary>
public sealed class OfflineBufferScenario : ITestScenario
{
    public string Name => "场景二：离线落库";

    private readonly FakeHttpClient      _httpClient;
    private readonly FakeDeviceService   _deviceService;
    private readonly DataPipelineService _pipeline;
    private readonly IFailedRecordStore  _failedStore;
    private readonly ICapacityBufferStore _bufferStore;
    private readonly ILogService         _logger;

    public OfflineBufferScenario(
        FakeHttpClient       httpClient,
        FakeDeviceService    deviceService,
        DataPipelineService  pipeline,
        IFailedRecordStore   failedStore,
        ICapacityBufferStore bufferStore,
        ILogService          logger)
    {
        _httpClient    = httpClient;
        _deviceService = deviceService;
        _pipeline      = pipeline;
        _failedStore   = failedStore;
        _bufferStore   = bufferStore;
        _logger        = logger;
    }

    public async Task<ScenarioResult> RunAsync(CancellationToken ct = default)
    {
        var assertions = new List<AssertionResult>();

        try
        {
            // ── 前置 ───────────────────────────────────────────
            _deviceService.CurrentState = NetworkState.Offline;
            _httpClient.IsOnline        = false;
            _httpClient.Reset();

            var random = Random.Shared;
            var recordCount = random.Next(5, 13); // [5,12]

            // ── 执行 ───────────────────────────────────────────
            for (int i = 1; i <= recordCount; i++)
            {
                _pipeline.Enqueue(BuildRandomRecord(i, random));

                // 增加轻微随机抖动，更贴近真实设备节拍
                if (random.NextDouble() < 0.35)
                    await Task.Delay(random.Next(5, 26), ct);
            }

            await WaitQueueEmptyAsync(ct);
            await Task.Delay(300, ct); // 等待所有 Store 写入落盘

            // ── 断言 ───────────────────────────────────────────
            var failedCount = await _failedStore.GetCountAsync("Cloud");
            var bufferCount = await _bufferStore.GetCountAsync();
            var callCount   = _httpClient.CallCount;

            assertions.Add(Assert($"FailedRecordStore(Cloud) == {recordCount}", failedCount == recordCount, recordCount.ToString(), failedCount.ToString()));
            assertions.Add(Assert($"CapacityBufferStore == {recordCount}",       bufferCount == recordCount, recordCount.ToString(), bufferCount.ToString()));
            assertions.Add(Assert("FakeHttpClient.CallCount == 0",               callCount   == 0, "0", callCount.ToString()));
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

    // ── 辅助方法 ────────────────────────────────────────────────

    private async Task WaitQueueEmptyAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (_pipeline.PendingCount > 0 && DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            await Task.Delay(80, ct);
    }

    private static CellCompletedRecord BuildRandomRecord(int index, Random random)
    {
        var now = DateTime.Now;
        var scanTime = now.AddMilliseconds(-random.Next(100, 5000));
        var pre = 24.5 + random.NextDouble() * 2.0;
        var volume = 1.8 + random.NextDouble() * 1.8;
        var ok = random.Next(0, 100) >= 12;

        return new CellCompletedRecord
        {
            CellData = new InjectionCellData
            {
                DeviceName          = "TestDevice",
                DeviceCode          = "SIM-001",
                Barcode             = $"OFFLINE-{index:D3}-{random.Next(1000, 9999)}",
                CellResult          = ok,
                CompletedTime       = now,
                ScanTime            = scanTime,
                PreInjectionWeight  = pre,
                PostInjectionWeight = pre + volume,
                InjectionVolume     = volume
            }
        };
    }

    private static AssertionResult Assert(string desc, bool passed, string expected, string actual)
        => new() { Description = desc, Passed = passed, Expected = expected, Actual = actual };
}
