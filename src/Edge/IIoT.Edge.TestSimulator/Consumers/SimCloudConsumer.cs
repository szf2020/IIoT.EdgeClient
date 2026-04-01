using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.TestSimulator.Consumers;

/// <summary>
/// 测试用云端消费者：直接调 FakeHttpClient，不需要 AutoMapper
/// 离线时返回 false → ProcessQueueTask 存入 FailedRecordStore
/// </summary>
public sealed class SimCloudConsumer : ICloudConsumer
{
    private readonly ICloudHttpClient _httpClient;
    private readonly IDeviceService   _deviceService;
    private readonly ILogService      _logger;

    public string  Name         => "Cloud";
    public int     Order        => 30;
    public string? RetryChannel => "Cloud";

    public SimCloudConsumer(
        ICloudHttpClient httpClient,
        IDeviceService   deviceService,
        ILogService      logger)
    {
        _httpClient    = httpClient;
        _deviceService = deviceService;
        _logger        = logger;
    }

    public async Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        var label = record.CellData.DisplayLabel;

        if (_deviceService.CurrentState == NetworkState.Offline)
        {
            _logger.Warn($"[SimCloud] 离线，{label} 转入重传队列");
            return false;
        }

        var success = await _httpClient.PostAsync("/api/test/passstation", record.CellData);

        if (success)
            _logger.Info($"[SimCloud] 上报成功: {label}");
        else
            _logger.Error($"[SimCloud] 上报失败: {label}");

        return success;
    }
}
