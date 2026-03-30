using AutoMapper;
using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.DataMapping.Cloud.Injection;
using System.Net.Http.Json;

namespace IIoT.Edge.CloudSync.PassStation;

/// <summary>
/// 云端过站数据上报消费者
/// 
/// 消费链 Order=20
/// 按工序类型分发到对应的 AutoMapper Profile 做转换
/// </summary>
public class CloudConsumer : ICloudConsumer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly IMapper _mapper;
    private readonly ILogService _logger;
    public string? RetryChannel => "Cloud";
    public string Name => "Cloud";
    public int Order => 20;

    public CloudConsumer(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        IMapper mapper,
        ILogService logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        var cellData = record.CellData;
        var label = cellData.DisplayLabel;

        var device = _deviceService.CurrentDevice;
        if (device is null)
        {
            _logger.Warn($"[Cloud] 设备未寻址，跳过上报，{label}");
            return true;
        }

        if (_deviceService.CurrentState == NetworkState.Offline)
        {
            _logger.Warn($"[Cloud] 网络离线，{label} 转入重传队列");
            return false;
        }

        try
        {
            var (url, dto) = MapToCloudDto(cellData, device.DeviceId);
            if (dto is null)
            {
                _logger.Error($"[Cloud] 不支持的工序类型: {cellData.ProcessType}，{label}");
                return false;
            }

            var client = _httpClientFactory.CreateClient("CloudApi");
            var response = await client
                .PostAsJsonAsync(url, dto)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.Error($"[Cloud] 上报失败，{label}，状态码: {response.StatusCode}，响应: {body}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.Error($"[Cloud] 上报超时，{label}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"[Cloud] 网络异常，{label}，{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 根据工序类型映射成对应的云端 DTO 和 API 路径
    /// 新增工序在 switch 里加 case
    /// </summary>
    private (string url, object? dto) MapToCloudDto(CellDataBase cellData, Guid cloudDeviceId)
    {
        return cellData switch
        {
            InjectionCellData injection => (
                "/api/v1/PassStation/injection",
                MapInjection(injection, cloudDeviceId)
            ),
            // DieCuttingCellData dieCutting => (...),
            _ => (string.Empty, null)
        };
    }

    private object MapInjection(InjectionCellData data, Guid cloudDeviceId)
    {
        var dto = _mapper.Map<InjectionCloudDto>(data);
        dto.DeviceId = cloudDeviceId;
        return dto;
    }
}