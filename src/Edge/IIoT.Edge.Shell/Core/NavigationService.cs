using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.PluginSystem;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IIoT.Edge.Shell.Core;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IViewRegistry _viewRegistry;
    private readonly Dictionary<string, FrameworkElement> _viewCache = new();

    public ViewModelBase? CurrentViewModel { get; private set; }
    public FrameworkElement? CurrentView { get; private set; }

    public event Action<ViewModelBase?>? Navigated;

    public NavigationService(IServiceProvider serviceProvider, IViewRegistry viewRegistry)
    {
        _serviceProvider = serviceProvider;
        _viewRegistry = viewRegistry;
    }

    public void NavigateTo(string viewId)
    {
        var viewModelType = _viewRegistry.GetViewModelType(viewId);
        if (viewModelType is null)
        {
            System.Diagnostics.Debug.WriteLine($"[Nav] 找不到 ViewId: {viewId}");
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService(viewModelType) as ViewModelBase;
        if (viewModel is null)
        {
            System.Diagnostics.Debug.WriteLine($"[Nav] Resolve失败: {viewModelType.Name}");
            return;
        }

        if (!_viewCache.TryGetValue(viewId, out var view))
        {
            var viewType = _viewRegistry.GetViewType(viewId);
            if (viewType is null)
            {
                System.Diagnostics.Debug.WriteLine($"[Nav] 找不到 ViewType: {viewId}");
                return;
            }

            view = Activator.CreateInstance(viewType) as FrameworkElement;
            if (view is null)
            {
                System.Diagnostics.Debug.WriteLine($"[Nav] 创建View失败: {viewType.Name}");
                return;
            }

            view.DataContext = viewModel;
            _viewCache[viewId] = view;
            System.Diagnostics.Debug.WriteLine($"[Nav] 首次创建View: {viewId}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[Nav] 命中View缓存: {viewId}");
        }

        CurrentViewModel = viewModel;
        CurrentView = view;

        _ = viewModel.OnActivatedAsync();
        Navigated?.Invoke(CurrentViewModel);
    }

    public void InvalidateCache(string viewId)
    {
        _viewCache.Remove(viewId);
    }

    public void ClearAllCache()
    {
        _viewCache.Clear();
    }
}
