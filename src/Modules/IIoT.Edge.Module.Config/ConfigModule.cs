using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.Module.Config.ParamView;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Config
{
    public class ConfigModule : IEdgeModule
    {
        public string ModuleName => "Config";

        public void ConfigureServices(
            IServiceCollection services)
        { }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterRoute("Config.ParamView",
                typeof(ParamViewPage),
                typeof(ParamViewWidget));

            registry.RegisterMenu(new MenuInfo
            {
                Title = "参数配置",
                WidgetId = "Config.ParamView",
                Icon = "Cog",
                Order = 5,
                RequiredPermission =
                    Permissions.ParamConfig
            });
        }

        public IEnumerable<MenuInfo> GetMenuItems()
            => Enumerable.Empty<MenuInfo>();
    }
}