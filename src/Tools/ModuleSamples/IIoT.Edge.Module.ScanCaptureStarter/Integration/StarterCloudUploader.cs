using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.Modules;
using IIoT.Edge.Integration.Contracts.Http;
using IIoT.Edge.Module.ScanCaptureStarter.Constants;
using IIoT.Edge.Module.ScanCaptureStarter.Payload;
using IIoT.Edge.SharedKernel.DataPipeline;
using Microsoft.Extensions.Configuration;

namespace IIoT.Edge.Module.ScanCaptureStarter.Integration;

public sealed class StarterCloudUploader : IProcessCloudUploader
{
    private const string UploadPath = "/api/v1/edge/pass-stations/starter-sample";

    private readonly ICloudHttpClient _cloudHttp;
    private readonly ILogService _logger;
    private readonly IConfiguration _configuration;
    private readonly IProductionContextStore _contextStore;

    public StarterCloudUploader(
        ICloudHttpClient cloudHttp,
        ILogService logger,
        IConfiguration configuration,
        IProductionContextStore contextStore)
    {
        _cloudHttp = cloudHttp;
        _logger = logger;
        _configuration = configuration;
        _contextStore = contextStore;
    }

    public string ProcessType => StarterModuleConstants.ProcessType;

    public ProcessUploadMode UploadMode => ProcessUploadMode.Single;

    public async Task<CloudCallResult> UploadAsync(
        ProcessCloudUploadContext context,
        IReadOnlyList<CellCompletedRecord> records,
        CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            return CloudCallResult.Success();
        }

        var isEnabled = _configuration.GetValue<bool>("Modules:ScanCaptureStarter:CloudUploadEnabled");
        if (!isEnabled)
        {
            var deviceName = ResolveDeviceName(records[0], context);
            const string errorMessage = "ScanCaptureStarter cloud upload is disabled by configuration.";
            UpdateDiagnostics(deviceName, false, StarterModuleConstants.CloudUploadDisabledStatus, errorMessage);
            _logger.Warn($"[Cloud] {errorMessage}");
            return CloudCallResult.Failure(CloudCallOutcome.Exception, "cloud_upload_disabled");
        }

        foreach (var record in records)
        {
            if (record.CellData is not StarterCellData starter)
            {
                var deviceName = ResolveDeviceName(record, context);
                var errorMessage =
                    $"Starter uploader received unexpected process type '{record.CellData.ProcessType}'.";
                UpdateDiagnostics(deviceName, true, StarterModuleConstants.CloudUploadFailedStatus, errorMessage);
                _logger.Error($"[Cloud] {errorMessage}");
                return CloudCallResult.Failure(CloudCallOutcome.Exception, "unexpected_process_type");
            }

            var payload = new
            {
                deviceId = context.Device.DeviceId,
                item = new
                {
                    barcode = starter.Barcode,
                    sequenceNo = starter.SequenceNo,
                    runtimeStatus = starter.RuntimeStatus,
                    deviceName = starter.DeviceName,
                    deviceCode = starter.DeviceCode,
                    plcDeviceId = starter.PlcDeviceId,
                    cellResult = ToCloudCellResult(starter.CellResult),
                    completedTime = starter.CompletedTime
                }
            };

            var result = await _cloudHttp.PostAsync(
                UploadPath,
                payload,
                new CloudRequestOptions
                {
                    IdempotencyKey = CloudIdempotencyKeyBuilder.ForRecord(
                        ProcessType,
                        nameof(StarterCloudUploader),
                        record)
                }).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage =
                    $"Cloud API returned failure for starter barcode '{starter.Barcode}'. Outcome:{result.Outcome}, Reason:{result.ReasonCode}.";
                UpdateDiagnostics(
                    ResolveDeviceName(record, context),
                    true,
                    StarterModuleConstants.CloudUploadFailedStatus,
                    errorMessage);
                _logger.Error($"[Cloud] {errorMessage}");
                return result;
            }

            UpdateDiagnostics(
                ResolveDeviceName(record, context),
                true,
                StarterModuleConstants.CloudUploadSuccessStatus,
                errorMessage: null);
        }

        return CloudCallResult.Success();
    }

    private static string ResolveDeviceName(CellCompletedRecord record, ProcessCloudUploadContext context)
        => string.IsNullOrWhiteSpace(record.CellData.DeviceName)
            ? context.Device.DeviceName
            : record.CellData.DeviceName;

    private static string ToCloudCellResult(bool? cellResult)
        => cellResult switch
        {
            true => "OK",
            false => "NG",
            _ => "Unknown"
        };

    private void UpdateDiagnostics(
        string deviceName,
        bool enabled,
        string status,
        string? errorMessage)
    {
        var productionContext = _contextStore.GetOrCreate(deviceName);
        productionContext.Set(StarterModuleConstants.CloudUploadEnabledKey, enabled);
        productionContext.Set(StarterModuleConstants.LastCloudUploadStatusKey, status);
        productionContext.Set(StarterModuleConstants.LastCloudUploadAtKey, DateTime.UtcNow);

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            productionContext.RemoveDeviceData(StarterModuleConstants.LastCloudUploadErrorKey);
            return;
        }

        productionContext.Set(StarterModuleConstants.LastCloudUploadErrorKey, errorMessage);
    }
}
