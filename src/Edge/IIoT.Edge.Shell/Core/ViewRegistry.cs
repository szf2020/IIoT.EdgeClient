using IIoT.Edge.UI.Shared.Modularity;

namespace IIoT.Edge.Shell.Core;

public class ViewRegistry : IViewRegistry
{
    private readonly Dictionary<string, Type> _viewModelMap = new();
    private readonly Dictionary<string, Type> _viewMap = new();
    private readonly List<MenuInfo> _menus = new();
    private readonly List<AnchorableInfo> _anchorables = new();

    public void RegisterRoute(string viewId, Type viewType, Type viewModelType)
    {
        _viewModelMap[viewId] = viewModelType;
        _viewMap[viewId] = viewType;
    }

    public void RegisterMenu(MenuInfo menuInfo)
    {
        _menus.Add(menuInfo);
    }

    public void RegisterAnchorable(AnchorableInfo info, Type viewType, Type viewModelType)
    {
        _anchorables.Add(info);
        _viewModelMap[info.ContentId] = viewModelType;
        _viewMap[info.ContentId] = viewType;
    }

    public Type? GetViewModelType(string viewId)
    {
        _viewModelMap.TryGetValue(viewId, out var type);
        return type;
    }

    public Type? GetViewType(string viewId)
    {
        _viewMap.TryGetValue(viewId, out var type);
        return type;
    }

    public IReadOnlyList<MenuInfo> GetAllMenus() => _menus.AsReadOnly();
    public IReadOnlyList<AnchorableInfo> GetAllAnchorables() => _anchorables.AsReadOnly();
}
