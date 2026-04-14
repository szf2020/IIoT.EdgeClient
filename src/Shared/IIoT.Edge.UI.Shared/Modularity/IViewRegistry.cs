namespace IIoT.Edge.UI.Shared.Modularity;

/// <summary>
/// 视图注册表契约。
/// 负责维护页面路由、菜单和停靠面板的注册信息。
/// </summary>
public interface IViewRegistry
{
    void RegisterRoute(string viewId, Type viewType, Type viewModelType);
    void RegisterMenu(MenuInfo menuInfo);
    void RegisterAnchorable(AnchorableInfo info, Type viewType, Type viewModelType);
    Type? GetViewModelType(string viewId);
    IReadOnlyList<MenuInfo> GetAllMenus();
    IReadOnlyList<AnchorableInfo> GetAllAnchorables();
    Type? GetViewType(string viewId);
}
