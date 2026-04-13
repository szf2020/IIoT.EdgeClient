using AutoMapper;
using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.DataMapping.Cloud.Injection;

namespace IIoT.Edge.CloudSync.PassStation;

/// <summary>
/// 云端过站数据上报消费者
/// 消费链 Order=20
/// </summary>
public class CloudConsumer : ICloudConsumer, ICloudBatchConsumer
{
    private readonly ICloudHttpClient _cloudHttp;
    private readonly ICloudApiEndpointProvider _endpointProvider;
    private readonly IDeviceService _deviceService;
    private readonly IMapper _mapper;
    private readonly ILogService _logger;

    public string? RetryChannel => "Cloud";
    public string Name => "Cloud";
    public int Order => 20;

    public CloudConsumer(
        ICloudHttpClient cloudHttp,
        ICloudApiEndpointProvider endpointProvider,
        IDeviceService deviceService,
        IMapper mapper,
        ILogService logger)
    {
        _cloudHttp = cloudHttp;
        _endpointProvider = endpointProvider;
        _deviceService = deviceService;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<bool> ProcessAsync(CellCompletedRecord record)
        => ProcessBatchAsync([record]);

    public async Task<bool> ProcessBatchAsync(IReadOnlyList<CellCompletedRecord> records)
    {
        if (records.Count == 0)
            return true;

        var device = _deviceService.CurrentDevice;
        if (device is null)
        {
            _logger.Warn("[Cloud] 设备未寻址，跳过上报");
            return true;
        }

        if (_deviceService.CurrentState == NetworkState.Offline)
        {
            _logger.Warn($"[Cloud] 网络离线，{records.Count} 条数据转入重传队列");
            return false;
        }

        var items = new List<InjectionCloudDto>(records.Count);

        foreach (var record in records)
        {
            if (record.CellData is not InjectionCellData injection)
            {
                _logger.Error($"[Cloud] 不支持的工序类型: {record.CellData.ProcessType}，{record.CellData.DisplayLabel}");
                return false;
            }

            items.Add(MapInjection(injection));
        }

        var payload = new
        {
            deviceId = device.DeviceId,
            items
        };

        var success = await _cloudHttp.PostAsync(_endpointProvider.GetPassStationInjectionBatchPath(), payload);

        if (success)
            return true;

        _logger.Error($"[Cloud] 批量上报失败，条数={records.Count}");
        return false;
    }

    private InjectionCloudDto MapInjection(InjectionCellData data)
    {
        return _mapper.Map<InjectionCloudDto>(data);
    }
}
