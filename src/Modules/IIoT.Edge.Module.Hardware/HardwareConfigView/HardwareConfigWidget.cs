// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Hardware/HardwareConfigView/HardwareConfigWidget.cs
//
// 修改点：
// 1. 构造注入由 ISender + IAuthService + IMapper 改为 ISender + IAuthService
//    （IMapper 已移入各 Handler）
// 2. LoadAllAsync / LoadIoMappingsAsync / SaveAsync 内部改为 _sender.Send(new XxxQuery/Command(...))
// 3. 其余 UI 属性、枚举绑定、分页命令、集合操作逻辑完全不变

using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Module.Hardware.HardwareConfigView.Models;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Hardware.HardwareConfigView;

public class HardwareConfigWidget : WidgetBase
{
    public override string WidgetId   => "Hardware.ConfigView";
    public override string WidgetName => "硬件配置";

    private readonly ISender      _sender;
    private readonly IAuthService _authService;

    private const int IoPageSize = 20;

    public IEnumerable<DeviceType> DeviceTypes => Enum.GetValues<DeviceType>();
    public IEnumerable<PlcType>    PlcTypes    => Enum.GetValues<PlcType>();

    public bool CanEdit => _authService.HasPermission(Permissions.HardwareConfig);

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public ObservableCollection<NetworkDeviceVm> NetworkDevices { get; } = new();
    public ObservableCollection<SerialDeviceVm>  SerialDevices  { get; } = new();
    public ObservableCollection<IoMappingVm>     IoMappings     { get; } = new();

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

    public ICommand AddNetworkDeviceCommand    { get; }
    public ICommand DeleteNetworkDeviceCommand { get; }
    public ICommand AddSerialDeviceCommand     { get; }
    public ICommand DeleteSerialDeviceCommand  { get; }
    public ICommand AddIoMappingCommand        { get; }
    public ICommand DeleteIoMappingCommand     { get; }
    public ICommand IoNextPageCommand          { get; }
    public ICommand IoPrevPageCommand          { get; }
    public ICommand SaveCommand                { get; }

    public HardwareConfigWidget(ISender sender, IAuthService authService)
    {
        _sender      = sender;
        _authService = authService;

        AddNetworkDeviceCommand    = new BaseCommand(_ => AddNetworkDevice(),  _ => CanEdit);
        DeleteNetworkDeviceCommand = new BaseCommand(DeleteNetworkDevice,       _ => CanEdit);
        AddSerialDeviceCommand     = new BaseCommand(_ => AddSerialDevice(),   _ => CanEdit);
        DeleteSerialDeviceCommand  = new BaseCommand(DeleteSerialDevice,        _ => CanEdit);
        AddIoMappingCommand        = new BaseCommand(_ => AddIoMapping(),      _ => CanEdit);
        DeleteIoMappingCommand     = new BaseCommand(DeleteIoMapping,           _ => CanEdit);
        IoNextPageCommand          = new AsyncCommand(() => IoNextPageAsync());
        IoPrevPageCommand          = new BaseCommand(_ => IoPrevPage(),        _ => IoPageIndex > 0);
        SaveCommand                = new AsyncCommand(SaveAsync);
    }

    public override async Task OnActivatedAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        var result = await _sender.Send(new LoadHardwareConfigQuery());

        NetworkDevices.Clear();
        foreach (var n in result.NetworkDevices) NetworkDevices.Add(n);

        SerialDevices.Clear();
        foreach (var s in result.SerialDevices) SerialDevices.Add(s);

        if (NetworkDevices.Count > 0)
            SelectedNetworkDevice = NetworkDevices[0];
    }

    private async Task LoadIoMappingsAsync()
    {
        if (SelectedNetworkDevice is null) return;

        var result = await _sender.Send(
            new LoadIoMappingsQuery(SelectedNetworkDevice.Id, IoPageIndex, IoPageSize));

        IoMappings.Clear();
        IoTotalCount = result.TotalCount;
        foreach (var item in result.Items) IoMappings.Add(item);
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

    private void AddNetworkDevice()
        => NetworkDevices.Add(new NetworkDeviceVm { DeviceType = DeviceType.PLC });

    private void DeleteNetworkDevice(object? param)
    {
        if (param is NetworkDeviceVm vm) NetworkDevices.Remove(vm);
    }

    private void AddSerialDevice()
        => SerialDevices.Add(new SerialDeviceVm());

    private void DeleteSerialDevice(object? param)
    {
        if (param is SerialDeviceVm vm) SerialDevices.Remove(vm);
    }

    private void AddIoMapping()
    {
        if (SelectedNetworkDevice is null) return;
        IoMappings.Add(new IoMappingVm
        {
            NetworkDeviceId = SelectedNetworkDevice.Id,
            Direction       = "Read",
            DataType        = "Int16",
            AddressCount    = 1
        });
    }

    private void DeleteIoMapping(object? param)
    {
        if (param is IoMappingVm vm) IoMappings.Remove(vm);
    }

    private async Task SaveAsync()
    {
        await _sender.Send(new SaveHardwareConfigCommand(
            NetworkDevices.ToList(),
            SerialDevices.ToList(),
            SelectedNetworkDevice?.Id ?? 0,
            IoMappings.ToList()));

        await LoadAllAsync();
    }
}
