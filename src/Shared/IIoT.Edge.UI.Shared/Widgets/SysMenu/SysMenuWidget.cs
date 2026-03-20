// 路径：src/Shared/IIoT.Edge.UI.Shared/Widgets/SysMenu/SysMenuWidget.cs
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.PluginSystem;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.UI.Shared.Widgets.SysMenu
{
    public class SysMenuWidget : WidgetBase
    {
        public override string WidgetId => "Core.SysMenu";
        public override string WidgetName => "系统导航菜单";

        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;

        public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

        private MenuItemViewModel? _selectedItem;
        public MenuItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public ICommand NavigateCommand { get; }
        public ICommand LoginCommand { get; }

        public string LoginButtonText => _authService.IsAuthenticated
            ? $"注销 ({_authService.CurrentUser?.DisplayName})"
            : "登录";

        public SysMenuWidget(INavigationService navigationService, IAuthService authService)
        {
            _navigationService = navigationService;
            _authService = authService;

            LayoutRow = 1;
            LayoutColumn = 0;

            NavigateCommand = new BaseCommand(ExecuteNavigate);
            LoginCommand = new BaseCommand(_ => ExecuteLogin());

            _authService.AuthStateChanged += _ =>
            {
                RefreshMenuPermissions();
                OnPropertyChanged(nameof(LoginButtonText));
            };

            BuildMenuItems();
        }

        private void BuildMenuItems()
        {
            var menus = new List<MenuInfo>
            {
                new() { Title = "生产数据", WidgetId = "Production.DataView",     Icon = "ChartBar",            Order = 1, RequiredPermission = "" },
                new() { Title = "产能查询", WidgetId = "Production.CapacityView", Icon = "ChartLine",           Order = 2, RequiredPermission = "" },
                new() { Title = "IO交互",   WidgetId = "Hardware.IOView",         Icon = "SwapHorizontal",      Order = 3, RequiredPermission = "" },
                new() { Title = "产品配方", WidgetId = "Formula.RecipeView",      Icon = "FileDocumentOutline", Order = 4, RequiredPermission = Permissions.RecipeRead },
                new() { Title = "参数配置", WidgetId = "Config.ParamView",        Icon = "Cog",                 Order = 5, RequiredPermission = Permissions.ParamConfig },
                new() { Title = "硬件配置", WidgetId = "Hardware.ConfigView",     Icon = "ServerNetwork",       Order = 6, RequiredPermission = Permissions.HardwareConfig },
            };

            MenuItems.Clear();
            foreach (var menu in menus.OrderBy(m => m.Order))
                MenuItems.Add(new MenuItemViewModel(menu, _authService));
        }

        private void RefreshMenuPermissions()
        {
            foreach (var item in MenuItems)
                item.RefreshPermission();
        }

        private void ExecuteNavigate(object? parameter)
        {
            if (parameter is not string widgetId) return;
            if (string.IsNullOrEmpty(widgetId)) return;

            var menuItem = MenuItems.FirstOrDefault(m => m.WidgetId == widgetId);
            if (menuItem is null || !menuItem.IsAccessible) return;

            if (SelectedItem is not null)
                SelectedItem.IsSelected = false;

            SelectedItem = menuItem;
            SelectedItem.IsSelected = true;

            _navigationService.NavigateTo(widgetId);
        }

        private void ExecuteLogin()
        {
            if (_authService.IsAuthenticated)
            {
                _authService.Logout();
            }
            else
            {
                // 打开 MainWindow 里定义的 DialogHost
                DialogHost.OpenDialogCommand.Execute(null, null);
            }
        }
    }
}