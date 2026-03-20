// 路径：src/Modules/IIoT.Edge.Module.Hardware/IOView/IOViewWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;

namespace IIoT.Edge.Module.Hardware.IOView
{
    public class IOViewWidget : WidgetBase
    {
        public override string WidgetId => "Hardware.IOView";
        public override string WidgetName => "IO交互";
        public string PageTitle => WidgetName;
    }
}