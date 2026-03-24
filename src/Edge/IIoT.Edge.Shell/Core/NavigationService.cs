using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.PluginSystem;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IIoT.Edge.Shell.Core;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IViewRegistry _viewRegistry;

    // ★ View 缓存：同一个 widgetId 只创建一次 View
    private readonly Dictionary<string, FrameworkElement> _viewCache = new();

    public WidgetBase? CurrentWidget { get; private set; }
    public FrameworkElement? CurrentView { get; private set; }

    public event Action<WidgetBase?>? Navigated;

    public NavigationService(
        IServiceProvider serviceProvider,
        IViewRegistry viewRegistry)
    {
        _serviceProvider = serviceProvider;
        _viewRegistry = viewRegistry;
    }

    public void NavigateTo(string widgetId)
    {
        // 1. 拿 Widget（Singleton，每次同一个实例）
        var widgetType = _viewRegistry.GetWidgetType(widgetId);
        if (widgetType is null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Nav] 找不到 WidgetId: {widgetId}");
            return;
        }

        var widget = _serviceProvider
            .GetRequiredService(widgetType) as WidgetBase;
        if (widget is null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Nav] Resolve失败: {widgetType.Name}");
            return;
        }

        // 2. 从缓存拿 View，没有才创建
        if (!_viewCache.TryGetValue(widgetId, out var view))
        {
            var viewType = _viewRegistry.GetViewType(widgetId);
            if (viewType is null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Nav] 找不到ViewType: {widgetId}");
                return;
            }

            view = Activator.CreateInstance(viewType)
                as FrameworkElement;
            if (view is null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Nav] 创建View失败: {viewType.Name}");
                return;
            }

            view.DataContext = widget;
            _viewCache[widgetId] = view;
            System.Diagnostics.Debug.WriteLine(
                $"[Nav] 首次创建View: {widgetId}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Nav] 命中View缓存: {widgetId}");
        }

        // 3. 切换当前视图
        CurrentWidget = widget;
        CurrentView = view;
        Navigated?.Invoke(CurrentWidget);
    }

    /// 强制清除某个 widgetId 的缓存（热重载用）
    public void InvalidateCache(string widgetId)
    {
        _viewCache.Remove(widgetId);
    }

    /// 清除所有缓存
    public void ClearAllCache()
    {
        _viewCache.Clear();
    }
}