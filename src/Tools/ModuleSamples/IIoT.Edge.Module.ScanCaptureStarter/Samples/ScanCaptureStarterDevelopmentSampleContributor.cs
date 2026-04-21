using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.Modules;
using IIoT.Edge.Domain.Hardware.Aggregates;
using IIoT.Edge.Module.Abstractions;
using IIoT.Edge.Module.ScanCaptureStarter.Constants;
using IIoT.Edge.Module.ScanCaptureStarter.Payload;
using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.SharedKernel.Repository;
using Microsoft.Extensions.Configuration;

namespace IIoT.Edge.Module.ScanCaptureStarter.Samples;

public sealed class ScanCaptureStarterDevelopmentSampleContributor : IDevelopmentSampleContributor
{
    private const string SampleRemark = "Development sample bootstrap";

    private readonly IConfiguration _configuration;
    private readonly IRepository<NetworkDeviceEntity> _networkDevices;
    private readonly IRepository<IoMappingEntity> _ioMappings;
    private readonly IProductionContextStore _contextStore;
    private readonly ILogService _logger;
    private readonly Dictionary<string, IModuleHardwareProfileProvider> _hardwareProfiles;
    private readonly ScanCaptureStarterDevelopmentSampleOptions _options = new();

    public ScanCaptureStarterDevelopmentSampleContributor(
        IConfiguration configuration,
        IRepository<NetworkDeviceEntity> networkDevices,
        IRepository<IoMappingEntity> ioMappings,
        IProductionContextStore contextStore,
        ILogService logger,
        IEnumerable<IModuleHardwareProfileProvider> hardwareProfiles)
    {
        _configuration = configuration;
        _networkDevices = networkDevices;
        _ioMappings = ioMappings;
        _contextStore = contextStore;
        _logger = logger;
        _hardwareProfiles = hardwareProfiles.ToDictionary(x => x.ModuleId, StringComparer.OrdinalIgnoreCase);

        configuration.GetSection(ScanCaptureStarterDevelopmentSampleOptions.SectionName).Bind(_options);
    }

    public string ModuleId => StarterModuleConstants.ModuleId;

    public async Task EnsureConfigurationSamplesAsync(CancellationToken cancellationToken = default)
    {
        if (!ShouldSeedStarterSamples())
        {
            return;
        }

        var existingStarterDevices = await _networkDevices.GetListAsync(
            x => x.DeviceType == DeviceType.PLC && x.ModuleId == StarterModuleConstants.ModuleId,
            cancellationToken).ConfigureAwait(false);

        var sampleDevice = existingStarterDevices.FirstOrDefault(x =>
            string.Equals(x.DeviceName, _options.StarterDeviceName, StringComparison.OrdinalIgnoreCase));

        if (existingStarterDevices.Count > 0 && sampleDevice is null)
        {
            _logger.Info("[DevSamples] Existing starter PLC configuration detected. Skip sample device bootstrap.");
            return;
        }

        if (sampleDevice is null)
        {
            var plcDefaults = GetStarterHardwareProfile().GetDefaultPlcSettings();
            var conflictingDevice = await _networkDevices.GetAsync(
                x => x.DeviceName == _options.StarterDeviceName,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (conflictingDevice is not null)
            {
                _logger.Warn(
                    $"[DevSamples] Skip starter sample bootstrap because device name '{_options.StarterDeviceName}' is already occupied by module '{conflictingDevice.ModuleId}'.");
                return;
            }

            sampleDevice = new NetworkDeviceEntity(
                _options.StarterDeviceName,
                DeviceType.PLC,
                _options.StarterIpAddress,
                _options.StarterPort > 0 ? _options.StarterPort : plcDefaults.Port1 ?? 102)
            {
                DeviceModel = string.IsNullOrWhiteSpace(_options.StarterPlcModel)
                    ? plcDefaults.DeviceModel
                    : _options.StarterPlcModel,
                ModuleId = StarterModuleConstants.ModuleId,
                ConnectTimeout = _options.StarterConnectTimeout > 0
                    ? _options.StarterConnectTimeout
                    : plcDefaults.ConnectTimeout ?? 3000,
                IsEnabled = true,
                Remark = SampleRemark
            };

            _networkDevices.Add(sampleDevice);
            await _networkDevices.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.Info(
                $"[DevSamples] Seeded development PLC device '{sampleDevice.DeviceName}' for module {StarterModuleConstants.ModuleId}.");
        }

        await EnsureSampleMappingsAsync(sampleDevice, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureRuntimeSamplesAsync(CancellationToken cancellationToken = default)
    {
        if (!ShouldSeedStarterSamples())
        {
            return;
        }

        var sampleDevice = await _networkDevices.GetAsync(
            x => x.DeviceType == DeviceType.PLC
                && x.ModuleId == StarterModuleConstants.ModuleId
                && x.DeviceName == _options.StarterDeviceName,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (sampleDevice is null)
        {
            return;
        }

        var context = _contextStore.GetOrCreate(sampleDevice.DeviceName);
        context.DeviceId = sampleDevice.Id;

        if (!context.CurrentCells.Values.OfType<StarterCellData>().Any())
        {
            var sampleCell = new StarterCellData
            {
                Barcode = _options.SampleBarcode,
                SequenceNo = _options.SampleSequence,
                RuntimeStatus = "DevelopmentSample",
                DeviceName = sampleDevice.DeviceName,
                DeviceCode = sampleDevice.DeviceName,
                PlcDeviceId = sampleDevice.Id,
                CellResult = true,
                CompletedTime = DateTime.UtcNow
            };

            context.AddCell(sampleCell.Barcode, sampleCell);
            _logger.Info(
                $"[DevSamples] Seeded starter runtime sample cell '{sampleCell.Barcode}' for '{sampleDevice.DeviceName}'.");
        }

        if (!context.Has(StarterModuleConstants.LastPublishedSequenceKey))
        {
            context.Set(StarterModuleConstants.LastPublishedSequenceKey, _options.SampleSequence);
        }

        if (!context.Has(StarterModuleConstants.LastPublishedBarcodeKey))
        {
            context.Set(StarterModuleConstants.LastPublishedBarcodeKey, _options.SampleBarcode);
        }
    }

    private async Task EnsureSampleMappingsAsync(
        NetworkDeviceEntity sampleDevice,
        CancellationToken cancellationToken)
    {
        var mappings = await _ioMappings.GetListAsync(
            x => x.NetworkDeviceId == sampleDevice.Id,
            cancellationToken).ConfigureAwait(false);

        var existingLabels = new HashSet<string>(
            mappings.Select(x => x.Label),
            StringComparer.OrdinalIgnoreCase);

        var templateEntries = GetStarterHardwareProfile().GetDefaultIoTemplate();
        var addedCount = 0;
        foreach (var mapping in BuildStarterMappings(sampleDevice.Id, templateEntries, existingLabels))
        {
            _ioMappings.Add(mapping);
            addedCount++;
        }

        if (addedCount == 0)
        {
            return;
        }

        await _ioMappings.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.Info(
            $"[DevSamples] Seeded {addedCount} IO mappings for '{sampleDevice.DeviceName}'.");
    }

    private bool ShouldSeedStarterSamples()
    {
        if (!_options.Enabled || !_options.SeedScanCaptureStarterModule)
        {
            return false;
        }

        if (!string.Equals(GetEnvironmentName(), "Development", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var enabledModules = _configuration
            .GetSection("Modules:Enabled")
            .Get<string[]>()
            ?? [];

        return enabledModules.Contains(StarterModuleConstants.ModuleId, StringComparer.OrdinalIgnoreCase);
    }

    private string GetEnvironmentName()
        => _configuration["Shell:Environment"]?.Trim()
            ?? "Production";

    private IModuleHardwareProfileProvider GetStarterHardwareProfile()
    {
        if (_hardwareProfiles.TryGetValue(StarterModuleConstants.ModuleId, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Development sample bootstrap requires a hardware profile provider for module '{StarterModuleConstants.ModuleId}'.");
    }

    private static List<IoMappingEntity> BuildStarterMappings(
        int networkDeviceId,
        IReadOnlyCollection<ModuleIoTemplateEntry> templateEntries,
        ISet<string> existingLabels)
    {
        return templateEntries
            .Where(x => !existingLabels.Contains(x.Label))
            .OrderBy(x => x.SortOrder)
            .Select(x => new IoMappingEntity(
                networkDeviceId,
                x.Label,
                x.PlcAddress,
                x.AddressCount,
                x.DataType,
                x.Direction)
            {
                SortOrder = x.SortOrder,
                Remark = SampleRemark
            })
            .ToList();
    }
}
