// 路径：src/Modules/IIoT.Edge.Module.Production/DataView/DataViewWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;

namespace IIoT.Edge.Module.Production.DataView
{
    public class DataViewWidget : WidgetBase
    {
        public override string WidgetId => "Production.DataView";
        public override string WidgetName => "生产数据";
        public string PageTitle => WidgetName;
    }
}