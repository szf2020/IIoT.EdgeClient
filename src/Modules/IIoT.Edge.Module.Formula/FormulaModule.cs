// 路径：src/Modules/IIoT.Edge.Module.Formula/FormulaModule.cs
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.Module.Formula.RecipeView;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Formula
{
    public class FormulaModule : IEdgeModule
    {
        public string ModuleName => "Formula";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<RecipeViewWidget>();
        }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterRoute(
                "Formula.RecipeView",
                typeof(PlaceholderView),
                typeof(RecipeViewWidget));
        }

        public IEnumerable<MenuInfo> GetMenuItems()
            => Enumerable.Empty<MenuInfo>();
    }
}