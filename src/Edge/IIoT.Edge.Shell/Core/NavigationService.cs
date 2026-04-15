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
        var registration = _viewRegistry.GetViewRegistration(viewId);
        if (registration is null)
        {
            System.Diagnostics.Debug.WriteLine($"[Nav] 找不到 ViewId: {viewId}");
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService(registration.ViewModelType) as ViewModelBase;
        if (viewModel is null)
        {
            System.Diagnostics.Debug.WriteLine($"[Nav] Resolve 失败: {registration.ViewModelType.Name}");
            return;
        }

        FrameworkElement view;
        if (registration.CacheView && _viewCache.TryGetValue(viewId, out var cachedView))
        {
            view = cachedView;
        }
        else
        {
            view = (FrameworkElement)ActivatorUtilities.CreateInstance(_serviceProvider, registration.ViewType);
            view.DataContext = viewModel;

            if (registration.CacheView)
            {
                _viewCache[viewId] = view;
            }
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
