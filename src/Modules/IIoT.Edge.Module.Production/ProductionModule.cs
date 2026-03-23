using IIoT.Edge.Contracts.Model;
using IIoT.Edge.Module.Production.CapacityView;
using IIoT.Edge.Module.Production.DataView;
using IIoT.Edge.Module.Production.Equipment;
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
            services.AddSingleton<EquipmentWidget>();
        }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterRoute("Production.DataView",
                typeof(PlaceholderView), typeof(DataViewWidget));
            registry.RegisterRoute("Production.CapacityView",
                typeof(PlaceholderView), typeof(CapacityViewWidget));
            registry.RegisterAnchorable(
                new AnchorableInfo
                {
                    Title = "设备信息",
                    ContentId = "Core.Equipment",
                    InitialPosition = AnchorablePosition.Right,
                    IsVisible = true
                },
                typeof(EquipmentView),
                typeof(EquipmentWidget));

            registry.RegisterMenu(new MenuInfo
            {
                Title = "生产数据",
                WidgetId = "Production.DataView",
                Icon = "ChartBar",
                Order = 1,
                RequiredPermission = ""
            });
            registry.RegisterMenu(new MenuInfo
            {
                Title = "产能查询",
                WidgetId = "Production.CapacityView",
                Icon = "ChartLine",
                Order = 2,
                RequiredPermission = ""
            });
        }

        public IEnumerable<MenuInfo> GetMenuItems()
            => Enumerable.Empty<MenuInfo>();
    }
}