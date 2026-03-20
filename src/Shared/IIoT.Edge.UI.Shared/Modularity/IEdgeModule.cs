using IIoT.Edge.Contracts.Model;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;

public interface IEdgeModule
{
    string ModuleName { get; }

    void ConfigureServices(IServiceCollection services);

    void ConfigureViews(IViewRegistry registry);

    // 返回该模块贡献的菜单项，没有就返回空集合
    IEnumerable<MenuInfo> GetMenuItems();
}