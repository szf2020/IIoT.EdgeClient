namespace IIoT.Edge.Module.Hardware.IOView;

/// <summary>
/// IO交互页设备选择用 ViewModel
/// 只暴露 UI 所需的最小字段，不持有 Domain Entity
/// </summary>
public class IoNetworkDeviceVm
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
}
