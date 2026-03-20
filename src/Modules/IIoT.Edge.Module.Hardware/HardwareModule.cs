// 路径：src/Modules/IIoT.Edge.Module.Hardware/HardwareModule.cs
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.Module.Hardware.HardwareConfigView;
using IIoT.Edge.Module.Hardware.IOView;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Hardware
{
    public class HardwareModule : IEdgeModule
    {
        public string ModuleName => "Hardware";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IOViewWidget>();
            services.AddTransient<HardwareConfigWidget>();
        }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterRoute(
                "Hardware.IOView",
                typeof(PlaceholderView),
                typeof(IOViewWidget));

            registry.RegisterRoute(
                "Hardware.ConfigView",
                typeof(PlaceholderView),
                typeof(HardwareConfigWidget));
        }

        public IEnumerable<MenuInfo> GetMenuItems()
            => Enumerable.Empty<MenuInfo>();
    }
}