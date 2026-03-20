// 路径：src/Modules/IIoT.Edge.Module.Hardware/HardwareConfigView/HardwareConfigWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;

namespace IIoT.Edge.Module.Hardware.HardwareConfigView
{
    public class HardwareConfigWidget : WidgetBase
    {
        public override string WidgetId => "Hardware.ConfigView";
        public override string WidgetName => "硬件配置";
        public string PageTitle => WidgetName;
    }
}