using IIoT.Edge.Application.Common.Models;

namespace IIoT.Edge.Application.Features.Config.ParamView.Models;

/// <summary>
/// 通用参数编辑项视图模型。
/// </summary>
public class GeneralParamVm : ObservableModelBase
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
