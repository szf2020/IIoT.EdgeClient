// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Config/ParamView/ParamViewWidget.cs
//
// 修改点：
// 1. 构造注入由 ISender + IMapper 改为仅 ISender（IMapper 已移入各 Handler）
// 2. 所有数据调用改为 _sender.Send(new XxxQuery/Command(...))
// 3. UI 属性、集合、命令定义完全不变

using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Module.Config.ParamView.Models;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Config.ParamView;

public class ParamViewWidget : WidgetBase
{
    public override string WidgetId   => "Config.ParamView";
    public override string WidgetName => "参数配置";

    private readonly ISender _sender;

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public ObservableCollection<GeneralParamVm>     GeneralParams     { get; } = new();
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

    public ICommand SaveCommand               { get; }
    public ICommand AddGeneralParamCommand    { get; }
    public ICommand DeleteGeneralParamCommand { get; }
    public ICommand AddDeviceParamCommand     { get; }
    public ICommand DeleteDeviceParamCommand  { get; }

    public ParamViewWidget(ISender sender)
    {
        _sender = sender;

        SaveCommand = new AsyncCommand(SaveAsync);

        AddGeneralParamCommand    = new BaseCommand(_ => GeneralParams.Add(new GeneralParamVm()));
        DeleteGeneralParamCommand = new BaseCommand(OnDeleteGeneralParam);

        AddDeviceParamCommand = new BaseCommand(_ =>
        {
            if (SelectedGroup != null)
                SelectedGroup.Params.Add(new DeviceParamVm());
        });
        DeleteDeviceParamCommand = new BaseCommand(OnDeleteDeviceParam);
    }

    public override async Task OnActivatedAsync()
    {
        await InitAsync();
    }

    private async Task InitAsync()
    {
        var result = await _sender.Send(new LoadParamViewQuery());

        GeneralParams.Clear();
        foreach (var vm in result.GeneralParams)
            GeneralParams.Add(vm);

        DeviceParamGroups.Clear();
        foreach (var header in result.DeviceGroups)
            DeviceParamGroups.Add(new DeviceParamGroupVm
            {
                DeviceId   = header.DeviceId,
                DeviceName = header.DeviceName
            });

        if (DeviceParamGroups.Count > 0)
            SelectedGroup = DeviceParamGroups[0];
    }

    private async Task LoadDeviceParamsAsync(DeviceParamGroupVm group)
    {
        var parms = await _sender.Send(new LoadDeviceParamsQuery(group.DeviceId));

        group.Params.Clear();
        foreach (var vm in parms) group.Params.Add(vm);
    }

    private void OnDeleteGeneralParam(object? param)
    {
        if (param is GeneralParamVm vm) GeneralParams.Remove(vm);
    }

    private void OnDeleteDeviceParam(object? param)
    {
        if (param is DeviceParamVm vm && SelectedGroup != null)
            SelectedGroup.Params.Remove(vm);
    }

    private async Task SaveAsync()
    {
        if (SelectedGroup is null) return;

        await _sender.Send(new SaveParamViewCommand(
            GeneralParams.ToList(),
            SelectedGroup.DeviceId,
            SelectedGroup.Params.ToList()));

        // 保存后重新加载（与原逻辑一致）
        await LoadDeviceParamsAsync(SelectedGroup);
        var result = await _sender.Send(new LoadParamViewQuery());
        GeneralParams.Clear();
        foreach (var vm in result.GeneralParams) GeneralParams.Add(vm);
    }
}
