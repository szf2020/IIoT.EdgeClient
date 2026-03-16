using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IIoT.Edge.Shell.Core
{
    public class NavigationService : INavigationService
    {
        private readonly IViewRegistry _registry;
        private readonly IServiceProvider _serviceProvider;
        private readonly MainWindowViewModel _shellViewModel;

        public NavigationService(IViewRegistry registry, IServiceProvider serviceProvider, MainWindowViewModel shellViewModel)
        {
            _registry = registry;
            _serviceProvider = serviceProvider;
            _shellViewModel = shellViewModel;
        }

        public void NavigateTo(string routeName)
        {
            if (_registry is ViewRegistry registryImpl && registryImpl.Routes.TryGetValue(routeName, out var routeInfo))
            {
                // 1. 从 DI 容器中解析 View 和 ViewModel
                var view = (FrameworkElement)_serviceProvider.GetRequiredService(routeInfo.ViewType);
                var viewModel = _serviceProvider.GetRequiredService(routeInfo.ViewModelType);

                // 2. 绑定 DataContext
                view.DataContext = viewModel;

                // 3. 替换主窗体的中心内容
                _shellViewModel.CurrentView = view;
            }
        }
    }
}