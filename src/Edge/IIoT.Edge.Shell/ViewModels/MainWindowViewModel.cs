// 路径：src/Edge/IIoT.Edge.Shell/ViewModels/MainWindowViewModel.cs
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Widgets.Login;
using IIoT.Edge.UI.Shared.Widgets.SysMenu;
using IIoT.Edge.UI.Shared.Widgets.SystemHeader;

namespace IIoT.Edge.Shell.ViewModels
{
    public class MainWindowViewModel : BaseNotifyPropertyChanged
    {
        public HeaderWidget HeaderViewModel { get; }
        public SysMenuWidget SysMenuViewModel { get; }
        public LoginWidget LoginViewModel { get; }

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public MainWindowViewModel(
            HeaderWidget headerWidget,
            SysMenuWidget sysMenuWidget,
            LoginWidget loginWidget,
            INavigationService navigationService)
        {
            HeaderViewModel = headerWidget;
            SysMenuViewModel = sysMenuWidget;
            LoginViewModel = loginWidget;

            // 监听导航事件，更新主工作区
            navigationService.Navigated += widget => CurrentView = widget;
        }
    }
}