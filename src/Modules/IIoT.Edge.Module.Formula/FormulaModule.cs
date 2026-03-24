using IIoT.Edge.Contracts.Model;
using IIoT.Edge.Module.Formula.RecipeView;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Formula
{
    public class FormulaModule : IEdgeModule
    {
        public string ModuleName => "Formula";

        public void ConfigureServices(
            IServiceCollection services)
        { }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterRoute("Formula.RecipeView",
                typeof(RecipeViewPage),
                typeof(RecipeViewWidget));

            registry.RegisterMenu(new MenuInfo
            {
                Title = "产品配方",
                WidgetId = "Formula.RecipeView",
                Icon = "FileDocumentOutline",
                Order = 4,
                RequiredPermission = ""
            });
        }

        public IEnumerable<MenuInfo> GetMenuItems()
            => Enumerable.Empty<MenuInfo>();
    }
}