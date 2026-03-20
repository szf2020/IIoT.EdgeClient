// 路径：src/Shared/IIoT.Edge.UI.Shared/Widgets/SysMenu/MenuItemViewModel.cs
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.UI.Shared.Mvvm;

namespace IIoT.Edge.UI.Shared.Widgets.SysMenu
{
    /// <summary>
    /// 单个菜单项的 ViewModel。
    /// IsEnabled 由 IAuthService.HasPermission 动态决定。
    /// </summary>
    public class MenuItemViewModel : BaseControlNotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly string _requiredPermission;

        public string Title { get; }
        public string WidgetId { get; }
        public string Icon { get; }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            private set { _isEnabled = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public MenuItemViewModel(MenuInfo info, IAuthService authService)
        {
            _authService = authService;
            _requiredPermission = info.RequiredPermission;

            Title = info.Title;
            WidgetId = info.WidgetId;
            Icon = info.Icon;

            RefreshPermission();
        }

        private bool _isAccessible;
        public bool IsAccessible
        {
            get => _isAccessible;
            private set { _isAccessible = value; OnPropertyChanged(); }
        }

        public void RefreshPermission()
        {
            IsAccessible = string.IsNullOrEmpty(_requiredPermission)
                || _authService.HasPermission(_requiredPermission);
        }
    }
}