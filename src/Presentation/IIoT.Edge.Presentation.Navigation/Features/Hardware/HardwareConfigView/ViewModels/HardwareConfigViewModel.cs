using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.UI.Shared.Mvvm;
using IIoT.Edge.Application.Common.Crud;
using IIoT.Edge.Application.Abstractions.Auth;
using IIoT.Edge.Application.Features.Hardware.HardwareConfigView;
using IIoT.Edge.Application.Features.Hardware.HardwareConfigView.Models;
using IIoT.Edge.Presentation.Navigation.Common.Crud;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Presentation.Navigation.Features.Hardware.HardwareConfigView;

/// <summary>
/// 硬件配置页面视图模型。
/// 负责网络设备、串口设备与 IO 映射的加载、分页和保存流程。
/// </summary>
public class HardwareConfigViewModel : CrudPageViewModelBase
{
    public override string ViewId => "Hardware.ConfigView";
    public override string ViewTitle => "硬件配置";

    private readonly IHardwareConfigCrudService _crudService;
    private readonly IAuthService _authService;
    private readonly IEditorValidator<NetworkDeviceVm> _networkDeviceValidator = new NetworkDeviceValidator();
    private readonly IEditorValidator<SerialDeviceVm> _serialDeviceValidator = new SerialDeviceValidator();
    private readonly IEditorValidator<IoMappingVm> _ioMappingValidator = new IoMappingValidator();

    private const int IoPageSize = 20;

    public IEnumerable<DeviceType> DeviceTypes => Enum.GetValues<DeviceType>();
    public IEnumerable<PlcType> PlcTypes => Enum.GetValues<PlcType>();

    public bool CanEdit => _authService.HasPermission(Permissions.HardwareConfig);

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public ObservableCollection<NetworkDeviceVm> NetworkDevices { get; } = new();
    public ObservableCollection<SerialDeviceVm> SerialDevices { get; } = new();
    public ObservableCollection<IoMappingVm> IoMappings { get; } = new();

    private NetworkDeviceVm? _selectedNetworkDevice;
    public NetworkDeviceVm? SelectedNetworkDevice
    {
        get => _selectedNetworkDevice;
        set
        {
            _selectedNetworkDevice = value;
            OnPropertyChanged();
            IoPageIndex = 0;
            _ = LoadIoMappingsAsync();
        }
    }

    private int _ioPageIndex;
    public int IoPageIndex
    {
        get => _ioPageIndex;
        set { _ioPageIndex = value; OnPropertyChanged(); }
    }

    private int _ioTotalCount;
    public int IoTotalCount
    {
        get => _ioTotalCount;
        set { _ioTotalCount = value; OnPropertyChanged(); }
    }

    public ICommand AddNetworkDeviceCommand { get; }
    public ICommand DeleteNetworkDeviceCommand { get; }
    public ICommand AddSerialDeviceCommand { get; }
    public ICommand DeleteSerialDeviceCommand { get; }
    public ICommand AddIoMappingCommand { get; }
    public ICommand DeleteIoMappingCommand { get; }
    public ICommand IoNextPageCommand { get; }
    public ICommand IoPrevPageCommand { get; }
    public ICommand SaveCommand { get; }

    public HardwareConfigViewModel(IHardwareConfigCrudService crudService, IAuthService authService)
    {
        _crudService = crudService;
        _authService = authService;

        AddNetworkDeviceCommand = CreateAddCommand(
            NetworkDevices,
            () => new NetworkDeviceVm { DeviceType = DeviceType.PLC },
            () => CanEdit);
        DeleteNetworkDeviceCommand = CreateDeleteCommand(NetworkDevices, () => CanEdit);
        AddSerialDeviceCommand = CreateAddCommand(
            SerialDevices,
            () => new SerialDeviceVm(),
            () => CanEdit);
        DeleteSerialDeviceCommand = CreateDeleteCommand(SerialDevices, () => CanEdit);
        AddIoMappingCommand = CreateScopedAddCommand(
            () => SelectedNetworkDevice is null ? null : IoMappings,
            () => new IoMappingVm
            {
                NetworkDeviceId = SelectedNetworkDevice!.Id,
                Direction = "Read",
                DataType = "Int16",
                AddressCount = 1
            },
            () => CanEdit && SelectedNetworkDevice is not null);
        DeleteIoMappingCommand = CreateDeleteCommand(IoMappings, () => CanEdit);
        IoNextPageCommand = new AsyncCommand(() => IoNextPageAsync());
        IoPrevPageCommand = new BaseCommand(_ => IoPrevPage(), _ => IoPageIndex > 0);
        SaveCommand = CreateBusyCommand(SaveAsync, () => CanEdit);
    }

    public override async Task OnActivatedAsync()
    {
        await ExecuteBusyAsync(LoadAllAsync);
    }

    private async Task LoadAllAsync()
    {
        var result = await _crudService.LoadAsync();

        ReplaceItems<NetworkDeviceVm>(NetworkDevices, result.NetworkDevices);
        ReplaceItems<SerialDeviceVm>(SerialDevices, result.SerialDevices);

        if (NetworkDevices.Count > 0)
            SelectedNetworkDevice = NetworkDevices[0];
    }

    private async Task LoadIoMappingsAsync()
    {
        if (SelectedNetworkDevice is null) return;

        var result = await _crudService.LoadIoMappingsAsync(
            SelectedNetworkDevice.Id,
            IoPageIndex,
            IoPageSize);

        IoTotalCount = result.TotalCount;
        ReplaceItems<IoMappingVm>(IoMappings, result.Items);
    }

    private async Task IoNextPageAsync()
    {
        if ((IoPageIndex + 1) * IoPageSize >= IoTotalCount) return;
        IoPageIndex++;
        await LoadIoMappingsAsync();
    }

    private void IoPrevPage()
    {
        if (IoPageIndex <= 0) return;
        IoPageIndex--;
        _ = LoadIoMappingsAsync();
    }

    private async Task<CrudOperationResult> SaveAsync()
    {
        var issues = new List<ValidationIssue>();
        issues.AddRange(await ValidateAsync(NetworkDevices, _networkDeviceValidator));
        issues.AddRange(await ValidateAsync(SerialDevices, _serialDeviceValidator));
        issues.AddRange(await ValidateAsync(IoMappings, _ioMappingValidator));

        var validationResult = CreateValidationResult(issues);
        if (!validationResult.IsSuccess)
            return validationResult;

        await _crudService.SaveAsync(
            NetworkDevices,
            SerialDevices,
            SelectedNetworkDevice?.Id ?? 0,
            IoMappings);

        await LoadAllAsync();

        return CrudOperationResult.Success("硬件配置已保存。");
    }
}

