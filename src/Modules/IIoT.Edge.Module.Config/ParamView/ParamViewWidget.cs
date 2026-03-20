// 路径：src/Modules/IIoT.Edge.Module.Config/ParamView/ParamViewWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;

namespace IIoT.Edge.Module.Config.ParamView
{
    public class ParamViewWidget : WidgetBase
    {
        public override string WidgetId => "Config.ParamView";
        public override string WidgetName => "参数配置";
        public string PageTitle => WidgetName;
    }
}