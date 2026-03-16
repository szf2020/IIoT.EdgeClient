// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/IViewRegistry.cs
using System;

namespace IIoT.Edge.UI.Shared.Modularity
{
    public interface IViewRegistry
    {
        void RegisterRoute(string routeName, Type viewType, Type viewModelType);

        void RegisterMenu(MenuInfo menuInfo);

        // 【新增】：注册动态面板
        void RegisterAnchorable(AnchorableInfo info, Type viewType, Type viewModelType);
    }
}