using IIoT.Edge.UI.Shared.Mvvm;

namespace IIoT.Edge.Presentation.Navigation.Features.Hardware.IOView;

/// <summary>
/// IO 信号展示模型。
/// 用于承载单个读写信号在界面上的地址、方向和当前值。
/// </summary>
public class IoSignalModel : BaseNotifyPropertyChanged
{
    public string Label { get; set; } = "";
    public string PlcAddress { get; set; } = "";
    public string Direction { get; set; } = "Read";
    public string DataType { get; set; } = "Int16";
    public int BufferIndex { get; set; }

    private int _value;
    public int Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }
}


