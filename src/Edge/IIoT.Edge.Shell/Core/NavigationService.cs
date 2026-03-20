// 路径：src/Edge/IIoT.Edge.Shell/Core/NavigationService.cs
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.PluginSystem;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Shell.Core
{
    /// <summary>
    /// INavigationService 的实现。
    /// 只操作 ViewModel，View 渲染完全交给 DataTemplate，这里永远不 new View。
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewRegistry _viewRegistry;

        public WidgetBase? CurrentWidget { get; private set; }

        public event Action<WidgetBase?>? Navigated;

        public NavigationService(IServiceProvider serviceProvider, IViewRegistry viewRegistry)
        {
            _serviceProvider = serviceProvider;
            _viewRegistry = viewRegistry;
        }

        /// <summary>
        /// 按 WidgetId 导航：
        /// 1. 从 ViewRegistry 查出对应的 ViewModel 类型
        /// 2. 从 DI 容器 Resolve 实例
        /// 3. 更新 CurrentWidget，触发 Navigated 事件
        /// </summary>
        public void NavigateTo(string widgetId)
        {
            var widgetType = _viewRegistry.GetWidgetType(widgetId);
            if (widgetType is null)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] 找不到 WidgetId: {widgetId}");
                return;
            }

            var widget = _serviceProvider.GetRequiredService(widgetType) as WidgetBase;
            if (widget is null)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Resolve 失败: {widgetType.Name}");
                return;
            }

            CurrentWidget = widget;
            Navigated?.Invoke(CurrentWidget);
        }
    }
}