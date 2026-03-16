// 路径：src/Edge/IIoT.Edge.Shell/Core/ViewRegistry.cs
using System;
using System.Collections.Generic;
using IIoT.Edge.UI.Shared.Modularity;

namespace IIoT.Edge.Shell.Core;

public class ViewRegistry : IViewRegistry
{
    // 存储 路由名称 -> (View类型, ViewModel类型)
    public Dictionary<string, (Type ViewType, Type ViewModelType)> Routes { get; } = new();

    // 存储收集到的左侧菜单
    public List<MenuInfo> Menus { get; } = new();

    public void RegisterRoute(string routeName, Type viewType, Type viewModelType)
    {
        Routes[routeName] = (viewType, viewModelType);
    }

    public void RegisterMenu(MenuInfo menuInfo)
    {
        Menus.Add(menuInfo);
    }
}