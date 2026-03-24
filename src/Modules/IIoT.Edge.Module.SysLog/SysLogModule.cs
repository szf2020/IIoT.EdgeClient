using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.SysLog
{
    public class SysLogModule : IEdgeModule
    {
        public string ModuleName => "SysLog";

        public void ConfigureServices(IServiceCollection services)
        {
            // 空 — 已移入 DependencyInjection.cs
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