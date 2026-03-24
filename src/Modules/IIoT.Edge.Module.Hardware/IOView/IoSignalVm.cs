using IIoT.Edge.Common.Mvvm;

namespace IIoT.Edge.Module.Hardware.IOView;

public class IoSignalVm : BaseNotifyPropertyChanged
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