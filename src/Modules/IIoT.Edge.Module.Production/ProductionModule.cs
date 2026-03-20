// 路径：src/Modules/IIoT.Edge.Module.Production/ProductionModule.cs
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.Module.Production.CapacityView;
using IIoT.Edge.Module.Production.DataView;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Production
{
    public class ProductionModule : IEdgeModule
    {
        public string ModuleName => "Production";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<DataViewWidget>();
            services.AddTransient<CapacityViewWidget>();
        }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterRoute(
                "Production.DataView",
                typeof(PlaceholderView),
                typeof(DataViewWidget));

            registry.RegisterRoute(
                "Production.CapacityView",
                typeof(PlaceholderView),
                typeof(CapacityViewWidget));
        }

        public IEnumerable<MenuInfo> GetMenuItems()
            => Enumerable.Empty<MenuInfo>();
    }
}