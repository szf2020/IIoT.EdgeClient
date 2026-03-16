// 路径：src/Edge/IIoT.Edge.Shell/Core/ViewRegistry.cs
using System;
using System.Collections.Generic;
using IIoT.Edge.UI.Shared.Modularity;

namespace IIoT.Edge.Shell.Core
{
    public class ViewRegistry : IViewRegistry
    {
        public Dictionary<string, (Type ViewType, Type ViewModelType)> Routes { get; } = new();
        public List<MenuInfo> Menus { get; } = new();

        // 【新增】：存储面板定义
        public List<(AnchorableInfo Info, Type ViewType, Type ViewModelType)> Anchorables { get; } = new();

        public void RegisterRoute(string routeName, Type viewType, Type viewModelType) => Routes[routeName] = (viewType, viewModelType);

        public void RegisterMenu(MenuInfo menuInfo) => Menus.Add(menuInfo);

        public void RegisterAnchorable(AnchorableInfo info, Type viewType, Type viewModelType)
        {
            Anchorables.Add((info, viewType, viewModelType));
        }
    }
}