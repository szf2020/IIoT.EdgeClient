// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/INavigationService.cs
namespace IIoT.Edge.UI.Shared.Modularity;

public interface INavigationService
{
    /// <summary>
    /// 导航到指定的路由页面
    /// </summary>
    /// <param name="routeName">在 IViewRegistry 中注册的路由名称</param>
    void NavigateTo(string routeName);
}