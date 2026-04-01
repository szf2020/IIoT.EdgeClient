using AutoMapper;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Domain.Config.Aggregates;
using IIoT.Edge.Module.Config.ParamView.Models;
using IIoT.Edge.Module.Config.UseCases.DeviceParam.Commands;
using IIoT.Edge.Module.Config.UseCases.DeviceParam.Queries;
using IIoT.Edge.Module.Config.UseCases.SystemConfig.Commands;
using IIoT.Edge.Module.Config.UseCases.SystemConfig.Queries;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Config.ParamView;

public class ParamViewWidget : WidgetBase
{
    public override string WidgetId => "Config.ParamView";
    public override string WidgetName => "参数配置";

    private readonly ISender _sender;
    private readonly IMapper _mapper;

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public ObservableCollection<GeneralParamVm> GeneralParams { get; } = new();
    public ObservableCollection<DeviceParamGroupVm> DeviceParamGroups { get; } = new();

    private DeviceParamGroupVm? _selectedGroup;
    public DeviceParamGroupVm? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            _selectedGroup = value;
            OnPropertyChanged();
            if (value != null) _ = LoadDeviceParamsAsync(value);
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand AddGeneralParamCommand { get; }
    public ICommand DeleteGeneralParamCommand { get; }
    public ICommand AddDeviceParamCommand { get; }
    public ICommand DeleteDeviceParamCommand { get; }

    public ParamViewWidget(
        ISender sender,
        IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;

        SaveCommand = new AsyncCommand(SaveAsync);
        AddGeneralParamCommand = new BaseCommand(
            _ => GeneralParams.Add(new GeneralParamVm()));
        DeleteGeneralParamCommand = new BaseCommand(OnDeleteGeneralParam);
        AddDeviceParamCommand = new BaseCommand(_ =>
        {
            if (SelectedGroup != null)
                SelectedGroup.Params.Add(new DeviceParamVm());
        });
        DeleteDeviceParamCommand = new BaseCommand(OnDeleteDeviceParam);
    }

    private async Task InitAsync()
    {
        await LoadGeneralParamsAsync();
        await LoadDeviceGroupsAsync();
    }

    private async Task LoadGeneralParamsAsync()
    {
        var result = await _sender.Send(new GetAllSystemConfigsQuery());

        GeneralParams.Clear();
        if (result.IsSuccess && result.Value != null)
        {
            foreach (var vm in result.Value
                .OrderBy(x => x.SortOrder)
                .Select(e => _mapper.Map<GeneralParamVm>(e)))
                GeneralParams.Add(vm);
        }
    }

    private async Task LoadDeviceGroupsAsync()
    {
        var result = await _sender.Send(new GetAllNetworkDevicesQuery());
        DeviceParamGroups.Clear();

        if (result.IsSuccess && result.Value != null)
        {
            foreach (var d in result.Value.Where(x => x.IsEnabled))
                DeviceParamGroups.Add(new DeviceParamGroupVm
                {
                    DeviceId = d.Id,
                    DeviceName = $"{d.DeviceName} ({d.IpAddress})"
                });
        }

        if (DeviceParamGroups.Count > 0)
            SelectedGroup = DeviceParamGroups[0];
    }

    private async Task LoadDeviceParamsAsync(DeviceParamGroupVm group)
    {
        var result = await _sender.Send(new GetDeviceParamsQuery(group.DeviceId));

        group.Params.Clear();
        if (result.IsSuccess && result.Value != null)
        {
            foreach (var vm in result.Value
                .OrderBy(x => x.SortOrder)
                .Select(e => _mapper.Map<DeviceParamVm>(e)))
                group.Params.Add(vm);
        }
    }

    private void OnDeleteGeneralParam(object? param)
    {
        if (param is GeneralParamVm vm)
            GeneralParams.Remove(vm);
    }

    private void OnDeleteDeviceParam(object? param)
    {
        if (param is DeviceParamVm vm && SelectedGroup != null)
            SelectedGroup.Params.Remove(vm);
    }
    public override async Task OnActivatedAsync()
    {
        await InitAsync();
    }
    private async Task SaveAsync()
    {
        // 通用参数：ViewModel → DTO
        var sysConfigs = GeneralParams
            .Select(vm => new SystemConfigDto(vm.Name, vm.Value, vm.Description))
            .ToList();

        await _sender.Send(new SaveSystemConfigsCommand(sysConfigs));

        // 设备参数：ViewModel → DTO
        if (SelectedGroup != null)
        {
            var deviceParams = SelectedGroup.Params
                .Select(vm => new DeviceParamDto(vm.Name, vm.Value, vm.Unit, vm.Min, vm.Max))
                .ToList();

            await _sender.Send(new SaveDeviceParamsCommand(
                SelectedGroup.DeviceId, deviceParams));
        }

        // 重新加载
        await LoadGeneralParamsAsync();
        if (SelectedGroup != null)
            await LoadDeviceParamsAsync(SelectedGroup);
    }
}