using AutoMapper;
using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.DataMapping.Cloud.Injection;

namespace IIoT.Edge.CloudSync.PassStation;

/// <summary>
/// 云端过站数据上报消费者
/// 消费链 Order=20
/// </summary>
public class CloudConsumer : ICloudConsumer
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly IDeviceService _deviceService;
    private readonly IMapper _mapper;
    private readonly ILogService _logger;

    public string? RetryChannel => "Cloud";
    public string Name => "Cloud";
    public int Order => 20;

    public CloudConsumer(
        ICloudHttpClient cloudHttp,
        IDeviceService deviceService,
        IMapper mapper,
        ILogService logger)
    {
        _cloudHttp = cloudHttp;
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

        var (url, dto) = MapToCloudDto(cellData, device.DeviceId);
        if (dto is null)
        {
            _logger.Error($"[Cloud] 不支持的工序类型: {cellData.ProcessType}，{label}");
            return false;
        }

        var success = await _cloudHttp.PostAsync(url, dto);

        if (success)
            return true;

        _logger.Error($"[Cloud] 上报失败，{label}");
        return false;
    }

    private (string url, object? dto) MapToCloudDto(CellDataBase cellData, Guid cloudDeviceId)
    {
        return cellData switch
        {
            InjectionCellData injection => (
                "/api/v1/PassStation/injection",
                MapInjection(injection, cloudDeviceId)
            ),
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