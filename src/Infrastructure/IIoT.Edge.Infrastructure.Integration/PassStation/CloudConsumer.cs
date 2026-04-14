using AutoMapper;
using IIoT.Edge.Application.Abstractions.DataPipeline.Consumers;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Infrastructure.Integration.Config;
using IIoT.Edge.Infrastructure.Integration.Mappings.Cloud.Injection;
using IIoT.Edge.SharedKernel.DataPipeline;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;

namespace IIoT.Edge.Infrastructure.Integration.PassStation;

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

    public CloudConsumer(ICloudHttpClient cloudHttp, ICloudApiEndpointProvider endpointProvider, IDeviceService deviceService, IMapper mapper, ILogService logger)
    {
        _cloudHttp = cloudHttp;
        _endpointProvider = endpointProvider;
        _deviceService = deviceService;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<bool> ProcessAsync(CellCompletedRecord record) => ProcessBatchAsync([record]);

    public async Task<bool> ProcessBatchAsync(IReadOnlyList<CellCompletedRecord> records)
    {
        if (records.Count == 0) return true;

        var device = _deviceService.CurrentDevice;
        if (device is null)
        {
            _logger.Warn("[Cloud] Device is not identified yet. Skip upload.");
            return true;
        }

        if (_deviceService.CurrentState == NetworkState.Offline)
        {
            _logger.Warn($"[Cloud] Network is offline. Move {records.Count} record(s) to retry queue.");
            return false;
        }

        var items = new List<InjectionCloudDto>(records.Count);
        foreach (var record in records)
        {
            if (record.CellData is not InjectionCellData injection)
            {
                _logger.Error($"[Cloud] Unsupported process type: {record.CellData.ProcessType}, Label:{record.CellData.DisplayLabel}");
                return false;
            }

            items.Add(_mapper.Map<InjectionCloudDto>(injection));
        }

        var payload = new
        {
            deviceId = device.DeviceId,
            items
        };

        var success = await _cloudHttp.PostAsync(_endpointProvider.GetPassStationInjectionBatchPath(), payload);
        if (success) return true;

        _logger.Error($"[Cloud] Batch upload failed. Count:{records.Count}");
        return false;
    }
}
