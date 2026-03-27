using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.DataMapping.Cloud;
using System.Net.Http.Json;

namespace IIoT.Edge.CloudSync.PassStation;

/// <summary>
/// 云端过站数据上报消费者
/// 
/// 消费链 Order=20（MES 之后，Excel 之前）
/// 
/// 业务逻辑：
///   CurrentDevice == null  → 从未寻址成功，跳过，return true
///   Offline                → 有 DeviceId 但网络断了，return false → 进重传队列
///   Online                 → 映射字段 → POST 云端 → 成功 true / 失败 false
/// </summary>
public class CloudConsumer : ICloudConsumer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly InjectionCloudMapper _cloudMapper;
    private readonly ILogService _logger;

    public string Name => "Cloud";
    public int Order => 20;

    public CloudConsumer(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        InjectionCloudMapper cloudMapper,
        ILogService logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _cloudMapper = cloudMapper;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        // ── 从未寻址成功，没有 DeviceId，跳过 ──────────────────
        var device = _deviceService.CurrentDevice;
        if (device is null)
        {
            _logger.Warn($"[Cloud] 设备未寻址，跳过上报，条码: {record.Barcode}");
            return true;
        }

        // ── 离线，有 DeviceId 但网络不通，进重传队列 ─────────────
        if (_deviceService.CurrentState == NetworkState.Offline)
        {
            _logger.Warn($"[Cloud] 网络离线，条码: {record.Barcode} 转入重传队列");
            return false;
        }

        // ── 在线，映射字段并上报 ─────────────────────────────────
        var dto = _cloudMapper.Map(record, device.DeviceId);
        if (dto is null)
        {
            _logger.Error($"[Cloud] 字段映射失败，条码: {record.Barcode}");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("CloudApi");
            var response = await client
                .PostAsJsonAsync("/api/v1/PassStation/injection", dto)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.Error($"[Cloud] 上报失败，条码: {record.Barcode}，" +
                $"状态码: {response.StatusCode}，响应: {body}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.Error($"[Cloud] 上报超时，条码: {record.Barcode}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"[Cloud] 网络异常，条码: {record.Barcode}，{ex.Message}");
            return false;
        }
    }
}