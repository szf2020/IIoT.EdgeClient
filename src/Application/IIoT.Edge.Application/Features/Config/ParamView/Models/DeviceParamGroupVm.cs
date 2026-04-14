using IIoT.Edge.Application.Common.Models;
using System.Collections.ObjectModel;

namespace IIoT.Edge.Application.Features.Config.ParamView.Models;

/// <summary>
/// 设备参数分组视图模型。
/// </summary>
public class DeviceParamGroupVm : ObservableModelBase
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public ObservableCollection<DeviceParamVm> Params { get; } = new();
}
