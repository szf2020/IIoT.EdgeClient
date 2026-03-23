// 路径：src/Edge/IIoT.Edge.Shell/ViewModels/MainWindowViewModel.cs
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Module.Production.Equipment;
using IIoT.Edge.Module.SysLog;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Widgets.Footer;
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
        public FooterWidget FooterViewModel { get; }
        public LogWidget LogViewModel { get; }
        public EquipmentWidget EquipmentViewModel { get; }

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
            FooterWidget footerWidget,
            LogWidget logWidget,
            EquipmentWidget equipmentWidget,
            INavigationService navigationService)
        {
            HeaderViewModel = headerWidget;
            SysMenuViewModel = sysMenuWidget;
            LoginViewModel = loginWidget;
            FooterViewModel = footerWidget;
            LogViewModel = logWidget;
            EquipmentViewModel = equipmentWidget;

            navigationService.Navigated += widget => CurrentView = widget;
        }
    }
}