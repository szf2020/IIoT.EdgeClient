using IIoT.Edge.Application.Abstractions.Auth;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Mvvm;

namespace IIoT.Edge.Presentation.Shell.Features.SysMenu;

/// <summary>
/// 系统菜单页。
/// 负责展示菜单信息和可访问状态。
/// </summary>
public class MenuItemViewModel : BaseControlNotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly string _requiredPermission;

    public string Title { get; }
    public string ViewId { get; }
    public string Icon { get; }

    private bool _isEnabled;
    public new bool IsEnabled
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
        ViewId = info.ViewId;
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
