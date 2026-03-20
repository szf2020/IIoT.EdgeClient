// 路径：src/Modules/IIoT.Edge.Module.SysLog/SysLogModule.cs
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;

namespace IIoT.Edge.Module.SysLog
{
    public class SysLogModule : IEdgeModule
    {
        public string ModuleName => "SysLog";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<LogWidget>();
        }

        public void ConfigureViews(IViewRegistry registry)
        {
            registry.RegisterAnchorable(
                new AnchorableInfo
                {
                    Title = "系统日志",
                    ContentId = "Core.SysLog",
                    InitialPosition = AnchorablePosition.Right,
                    IsVisible = true
                },
                typeof(LogView),
                typeof(LogWidget));
        }

        public IEnumerable<MenuInfo> GetMenuItems()
    => Enumerable.Empty<MenuInfo>();
    }
}