using AutoMapper;
using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Module.Hardware.HardwareConfigView.Models;
using IIoT.Edge.Module.Hardware.UseCases.IoMapping.Commands;
using IIoT.Edge.Module.Hardware.UseCases.IoMapping.Queries;
using IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Commands;
using IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Queries;
using IIoT.Edge.Module.Hardware.UseCases.SerialDevice.Commands;
using IIoT.Edge.Module.Hardware.UseCases.SerialDevice.Queries;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Hardware.HardwareConfigView;

public class HardwareConfigWidget : WidgetBase
{
    public override string WidgetId => "Hardware.ConfigView";
    public override string WidgetName => "硬件配置";

    private readonly ISender _sender;
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;

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

    public HardwareConfigWidget(
        ISender sender,
        IAuthService authService,
        IMapper mapper)
    {
        _sender = sender;
        _authService = authService;
        _mapper = mapper;

        AddNetworkDeviceCommand = new BaseCommand(_ => AddNetworkDevice(), _ => CanEdit);
        DeleteNetworkDeviceCommand = new BaseCommand(DeleteNetworkDevice, _ => CanEdit);
        AddSerialDeviceCommand = new BaseCommand(_ => AddSerialDevice(), _ => CanEdit);
        DeleteSerialDeviceCommand = new BaseCommand(DeleteSerialDevice, _ => CanEdit);
        AddIoMappingCommand = new BaseCommand(_ => AddIoMapping(), _ => CanEdit);
        DeleteIoMappingCommand = new BaseCommand(DeleteIoMapping, _ => CanEdit);
        IoNextPageCommand = new AsyncCommand(() => IoNextPageAsync());
        IoPrevPageCommand = new BaseCommand(_ => IoPrevPage(), _ => IoPageIndex > 0);
        SaveCommand = new AsyncCommand(SaveAsync);

        _ = LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        var networkResult = await _sender.Send(new GetAllNetworkDevicesQuery());
        NetworkDevices.Clear();
        if (networkResult.IsSuccess && networkResult.Value != null)
        {
            foreach (var n in networkResult.Value)
                NetworkDevices.Add(_mapper.Map<NetworkDeviceVm>(n));
        }

        var serialResult = await _sender.Send(new GetAllSerialDevicesQuery());
        SerialDevices.Clear();
        if (serialResult.IsSuccess && serialResult.Value != null)
        {
            foreach (var s in serialResult.Value)
                SerialDevices.Add(_mapper.Map<SerialDeviceVm>(s));
        }

        if (NetworkDevices.Count > 0)
            SelectedNetworkDevice = NetworkDevices[0];
    }

    private async Task LoadIoMappingsAsync()
    {
        if (SelectedNetworkDevice is null) return;

        var result = await _sender.Send(new GetIoMappingsByDeviceQuery(
            SelectedNetworkDevice.Id, IoPageIndex, IoPageSize));

        IoMappings.Clear();
        if (result.IsSuccess && result.Value != null)
        {
            IoTotalCount = result.Value.TotalCount;
            foreach (var item in result.Value.Items)
                IoMappings.Add(_mapper.Map<IoMappingVm>(item));
        }
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
        if (param is NetworkDeviceVm vm)
            NetworkDevices.Remove(vm);
    }

    private void AddSerialDevice()
        => SerialDevices.Add(new SerialDeviceVm());

    private void DeleteSerialDevice(object? param)
    {
        if (param is SerialDeviceVm vm)
            SerialDevices.Remove(vm);
    }

    private void AddIoMapping()
    {
        if (SelectedNetworkDevice is null) return;
        IoMappings.Add(new IoMappingVm
        {
            NetworkDeviceId = SelectedNetworkDevice.Id,
            Direction = "Read",
            DataType = "Int16",
            AddressCount = 1
        });
    }

    private void DeleteIoMapping(object? param)
    {
        if (param is IoMappingVm vm)
            IoMappings.Remove(vm);
    }

    private async Task SaveAsync()
    {
        // 网络设备：ViewModel → DTO
        var networkDtos = NetworkDevices
            .Select(vm => new NetworkDeviceDto(
                vm.Id, vm.DeviceName, vm.DeviceType,
                vm.DeviceModel, vm.IpAddress, vm.Port1, vm.IsEnabled))
            .ToList();
        await _sender.Send(new SaveNetworkDevicesCommand(networkDtos));

        // 串口设备：ViewModel → DTO
        var serialDtos = SerialDevices
            .Select(vm => new SerialDeviceDto(
                vm.Id, vm.DeviceName, vm.DeviceType,
                vm.PortName, vm.BaudRate, vm.IsEnabled))
            .ToList();
        await _sender.Send(new SaveSerialDevicesCommand(serialDtos));

        // IO映射：ViewModel → DTO
        if (SelectedNetworkDevice is not null)
        {
            var ioDtos = IoMappings
                .Select(vm => new IoMappingDto(
                    vm.Id, SelectedNetworkDevice.Id, vm.Label,
                    vm.PlcAddress, vm.AddressCount, vm.DataType,
                    vm.Direction, vm.SortOrder))
                .ToList();
            await _sender.Send(new SaveIoMappingsCommand(
                SelectedNetworkDevice.Id, ioDtos));
        }

        // 重新加载
        await LoadAllAsync();
    }
}