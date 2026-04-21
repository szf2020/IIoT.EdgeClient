using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.Modules;
using IIoT.Edge.Domain.Hardware.Aggregates;
using IIoT.Edge.Module.ScanCaptureStarter.Constants;
using IIoT.Edge.Module.ScanCaptureStarter.Payload;
using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.UI.Shared.PluginSystem;
using Microsoft.Extensions.Configuration;
using System.Windows.Threading;

namespace IIoT.Edge.Module.ScanCaptureStarter.Presentation.ViewModels;

public sealed class StarterSkeletonViewModel : PresentationViewModelBase
{
    private readonly IConfiguration _configuration;
    private readonly IReadRepository<NetworkDeviceEntity> _networkDevices;
    private readonly IProductionContextStore _contextStore;
    private readonly IStationRuntimeRegistry _runtimeRegistry;
    private readonly ICloudRetryRecordStore _cloudRetryRecordStore;
    private readonly DispatcherTimer _refreshTimer;

    public override string ViewId => StarterViewIds.Skeleton;

    public override string ViewTitle => "Starter Dashboard";

    public string ModuleId => StarterModuleConstants.ModuleId;

    private string _moduleStatus = "Checking module configuration...";
    public string ModuleStatus
    {
        get => _moduleStatus;
        private set
        {
            _moduleStatus = value;
            OnPropertyChanged();
        }
    }

    private string _deviceBindingStatus = "Checking device bindings...";
    public string DeviceBindingStatus
    {
        get => _deviceBindingStatus;
        private set
        {
            _deviceBindingStatus = value;
            OnPropertyChanged();
        }
    }

    private string _runtimeBindingStatus = "Checking runtime registration...";
    public string RuntimeBindingStatus
    {
        get => _runtimeBindingStatus;
        private set
        {
            _runtimeBindingStatus = value;
            OnPropertyChanged();
        }
    }

    private string _sampleDataSummary = "Checking runtime sample data...";
    public string SampleDataSummary
    {
        get => _sampleDataSummary;
        private set
        {
            _sampleDataSummary = value;
            OnPropertyChanged();
        }
    }

    private string _cloudUploadStatus = "Checking starter cloud upload...";
    public string CloudUploadStatus
    {
        get => _cloudUploadStatus;
        private set
        {
            _cloudUploadStatus = value;
            OnPropertyChanged();
        }
    }

    private string _cloudRetryStatus = "Checking pending cloud retries...";
    public string CloudRetryStatus
    {
        get => _cloudRetryStatus;
        private set
        {
            _cloudRetryStatus = value;
            OnPropertyChanged();
        }
    }

    public StarterSkeletonViewModel(
        IConfiguration configuration,
        IReadRepository<NetworkDeviceEntity> networkDevices,
        IProductionContextStore contextStore,
        IStationRuntimeRegistry runtimeRegistry,
        ICloudRetryRecordStore cloudRetryRecordStore)
    {
        _configuration = configuration;
        _networkDevices = networkDevices;
        _contextStore = contextStore;
        _runtimeRegistry = runtimeRegistry;
        _cloudRetryRecordStore = cloudRetryRecordStore;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _refreshTimer.Tick += (_, _) => RunViewTaskInBackground(RefreshAsync, "Starter diagnostics refresh failed");
        _refreshTimer.Start();
    }

    public override async Task OnActivatedAsync()
    {
        await RunViewTaskAsync(RefreshAsync, "Starter diagnostics load failed");
    }

    private async Task RefreshAsync()
    {
        var enabledModuleIds = _configuration
            .GetSection("Modules:Enabled")
            .GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        var isEnabled = enabledModuleIds.Contains(StarterModuleConstants.ModuleId, StringComparer.OrdinalIgnoreCase);
        ModuleStatus = isEnabled
            ? "Enabled in the current configuration."
            : "Not enabled in the current configuration.";

        var device = await _networkDevices.GetAsync(
            x => x.DeviceType == DeviceType.PLC
                && x.ModuleId == StarterModuleConstants.ModuleId);

        DeviceBindingStatus = device is null
            ? "No starter PLC device is configured yet."
            : $"Bound device: {device.DeviceName} ({device.IpAddress}:{device.Port1})";

        if (!_runtimeRegistry.HasFactory(StarterModuleConstants.ModuleId))
        {
            RuntimeBindingStatus = "Runtime factory is not registered.";
        }
        else if (device is null)
        {
            RuntimeBindingStatus = $"Factory registered: {StarterModuleConstants.SignalTaskName}. Waiting for a bound device.";
        }
        else
        {
            var ctx = _contextStore.GetAll().FirstOrDefault(x => x.DeviceName == device.DeviceName);
            var runtimeMarkedActive = ctx?.Get<bool>(StarterModuleConstants.RuntimeRegisteredKey) == true;
            RuntimeBindingStatus = runtimeMarkedActive
                ? $"Factory registered and runtime context is active: {StarterModuleConstants.SignalTaskName}."
                : $"Factory registered: {StarterModuleConstants.SignalTaskName}. Waiting for PLC startup.";
        }

        var targetContext = device is null
            ? null
            : _contextStore.GetAll().FirstOrDefault(x => x.DeviceName == device.DeviceName);

        var latestCell = targetContext?.CurrentCells.Values
            .OfType<StarterCellData>()
            .OrderByDescending(x => x.SequenceNo)
            .ThenByDescending(x => x.CompletedTime)
            .FirstOrDefault();

        SampleDataSummary = latestCell is null
            ? "No starter sample cell is available yet."
            : $"Latest sample: {latestCell.Barcode}, Sequence:{latestCell.SequenceNo}, Result:{ToDisplayResult(latestCell.CellResult)}, Status:{latestCell.RuntimeStatus}.";

        var cloudEnabled = bool.TryParse(
            _configuration["Modules:ScanCaptureStarter:CloudUploadEnabled"],
            out var enabledValue)
            && enabledValue;
        var lastCloudStatus = targetContext?.Get<string>(StarterModuleConstants.LastCloudUploadStatusKey);
        var lastCloudError = targetContext?.Get<string>(StarterModuleConstants.LastCloudUploadErrorKey);

        CloudUploadStatus = cloudEnabled
            ? string.IsNullOrWhiteSpace(lastCloudStatus)
                ? "Cloud upload is enabled for the starter module."
                : $"Cloud upload status: {lastCloudStatus}. {(string.IsNullOrWhiteSpace(lastCloudError) ? string.Empty : lastCloudError)}".Trim()
            : "Cloud upload is disabled for the starter module in the current configuration.";

        var pendingCloudRetries = await _cloudRetryRecordStore.GetCountAsync(StarterModuleConstants.ProcessType);
        CloudRetryStatus = $"Pending Cloud retry records for starter module: {pendingCloudRetries}.";

        SetStatus("Starter module shows the minimum scan-capture-upload path for new project onboarding.");
    }

    private static string ToDisplayResult(bool? cellResult)
        => cellResult switch
        {
            true => "OK",
            false => "NG",
            _ => "Unknown"
        };
}
