using System;

namespace IIoT.Edge.UI.Shared.Modularity
{
    public interface IViewRegistry
    {
        void RegisterRoute(string routeName, Type viewType, Type viewModelType);

        void RegisterMenu(MenuInfo menuInfo);
    }
}