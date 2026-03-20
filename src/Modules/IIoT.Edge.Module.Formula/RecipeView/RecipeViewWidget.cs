// 路径：src/Modules/IIoT.Edge.Module.Formula/RecipeView/RecipeViewWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;

namespace IIoT.Edge.Module.Formula.RecipeView
{
    public class RecipeViewWidget : WidgetBase
    {
        public override string WidgetId => "Formula.RecipeView";
        public override string WidgetName => "产品配方";
        public string PageTitle => WidgetName;
    }
}