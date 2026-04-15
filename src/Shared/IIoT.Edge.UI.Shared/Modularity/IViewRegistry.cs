namespace IIoT.Edge.UI.Shared.Modularity;

public interface IViewRegistry
{
    void RegisterRoute(string viewId, Type viewType, Type viewModelType, bool cacheView = true);
    void RegisterMenu(MenuInfo menuInfo);
    void RegisterAnchorable(AnchorableInfo info, Type viewType, Type viewModelType, bool cacheView = true);
    ViewRegistration? GetViewRegistration(string viewId);
    IReadOnlyList<MenuInfo> GetAllMenus();
    IReadOnlyList<AnchorableInfo> GetAllAnchorables();
}
