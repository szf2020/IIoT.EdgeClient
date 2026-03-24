using IIoT.Edge.Common.Mvvm;

namespace IIoT.Edge.Module.Config.ParamView.Models;

public class GeneralParamVm : BaseNotifyPropertyChanged
{
    public int Id { get; set; }
    public string Key { get; set; } = "";

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    private string _value = "";
    public string Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public string Description { get; set; } = "";
}