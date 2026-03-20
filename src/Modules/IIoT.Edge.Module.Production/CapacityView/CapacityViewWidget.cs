// 路径：src/Modules/IIoT.Edge.Module.Production/CapacityView/CapacityViewWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;

namespace IIoT.Edge.Module.Production.CapacityView
{
    public class CapacityViewWidget : WidgetBase
    {
        public override string WidgetId => "Production.CapacityView";
        public override string WidgetName => "产能查询";
        public string PageTitle => WidgetName;
    }
}