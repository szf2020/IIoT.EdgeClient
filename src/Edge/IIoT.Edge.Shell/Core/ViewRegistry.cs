using IIoT.Edge.UI.Shared.Modularity;

namespace IIoT.Edge.Shell.Core;

public class ViewRegistry : IViewRegistry
{
    private readonly Dictionary<string, ViewRegistration> _views = new();
    private readonly List<MenuInfo> _menus = new();
    private readonly List<AnchorableInfo> _anchorables = new();

    public void RegisterRoute(string viewId, Type viewType, Type viewModelType, bool cacheView = true)
    {
        _views[viewId] = new ViewRegistration
        {
            ViewId = viewId,
            ViewType = viewType,
            ViewModelType = viewModelType,
            CacheView = cacheView
        };
    }

    public void RegisterMenu(MenuInfo menuInfo)
    {
        _menus.Add(menuInfo);
    }

    public void RegisterAnchorable(AnchorableInfo info, Type viewType, Type viewModelType, bool cacheView = true)
    {
        _anchorables.Add(info);
        _views[info.ContentId] = new ViewRegistration
        {
            ViewId = info.ContentId,
            ViewType = viewType,
            ViewModelType = viewModelType,
            CacheView = cacheView
        };
    }

    public ViewRegistration? GetViewRegistration(string viewId)
    {
        _views.TryGetValue(viewId, out var registration);
        return registration;
    }

    public IReadOnlyList<MenuInfo> GetAllMenus() => _menus.AsReadOnly();

    public IReadOnlyList<AnchorableInfo> GetAllAnchorables() => _anchorables.AsReadOnly();
}
