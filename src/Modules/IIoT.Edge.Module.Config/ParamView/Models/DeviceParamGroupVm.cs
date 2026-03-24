using IIoT.Edge.Common.Mvvm;
using System.Collections.ObjectModel;

namespace IIoT.Edge.Module.Config.ParamView.Models;

public class DeviceParamGroupVm : BaseNotifyPropertyChanged
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public ObservableCollection<DeviceParamVm> Params { get; } = new();
}