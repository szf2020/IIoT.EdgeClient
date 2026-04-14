using IIoT.Edge.Application.Abstractions.Auth;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.Presentation.Navigation.Features.Config.ParamView;
using IIoT.Edge.Presentation.Navigation.Features.Formula.RecipeView;
using IIoT.Edge.Presentation.Navigation.Features.Hardware.HardwareConfigView;
using IIoT.Edge.Presentation.Navigation.Features.Hardware.IOView;
using IIoT.Edge.Presentation.Navigation.Features.Production.CapacityView;
using IIoT.Edge.Presentation.Navigation.Features.Production.DataView;
using IIoT.Edge.Presentation.Navigation.Features.Production.Monitor;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Presentation.Navigation;

/// <summary>
/// Navigation 层依赖注入与路由注册扩展。
/// 负责注册左侧导航页的视图模型、路由和菜单项。
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册导航层视图模型。
    /// </summary>
    public static IServiceCollection AddNavigationPresentation(this IServiceCollection services)
    {
        services.AddSingleton<ParamViewModel>();
        services.AddSingleton<IoViewViewModel>();
        services.AddSingleton<HardwareConfigViewModel>();
        services.AddSingleton<RecipeViewModel>();
        services.AddSingleton<CapacityViewModel>();
        services.AddSingleton<MonitorViewModel>();
        services.AddSingleton<DataViewModel>();
        return services;
    }

    /// <summary>
    /// 注册导航页面路由与菜单定义。
    /// </summary>
    public static IViewRegistry RegisterNavigationViews(this IViewRegistry registry)
    {
        registry.RegisterRoute("Production.DataView", typeof(DataViewPage), typeof(DataViewModel));
        registry.RegisterRoute("Production.CapacityView", typeof(CapacityViewPage), typeof(CapacityViewModel));
        registry.RegisterRoute("Production.Monitor", typeof(MonitorViewPage), typeof(MonitorViewModel));
        registry.RegisterRoute("Hardware.IOView", typeof(IOViewPage), typeof(IoViewViewModel));
        registry.RegisterRoute("Formula.RecipeView", typeof(RecipeViewPage), typeof(RecipeViewModel));
        registry.RegisterRoute("Config.ParamView", typeof(ParamViewPage), typeof(ParamViewModel));
        registry.RegisterRoute("Hardware.ConfigView", typeof(HardwareConfigPage), typeof(HardwareConfigViewModel));

        registry.RegisterMenu(new MenuInfo { Title = "生产数据", ViewId = "Production.DataView", Icon = "ChartBar", Order = 1, RequiredPermission = string.Empty });
        registry.RegisterMenu(new MenuInfo { Title = "产能查询", ViewId = "Production.CapacityView", Icon = "ChartLine", Order = 2, RequiredPermission = string.Empty });
        registry.RegisterMenu(new MenuInfo { Title = "IO交互", ViewId = "Hardware.IOView", Icon = "SwapHorizontal", Order = 3, RequiredPermission = string.Empty });
        registry.RegisterMenu(new MenuInfo { Title = "实时监控", ViewId = "Production.Monitor", Icon = "MonitorDashboard", Order = 4, RequiredPermission = string.Empty });
        registry.RegisterMenu(new MenuInfo { Title = "产品配方", ViewId = "Formula.RecipeView", Icon = "FileDocumentOutline", Order = 5, RequiredPermission = string.Empty });
        registry.RegisterMenu(new MenuInfo { Title = "参数配置", ViewId = "Config.ParamView", Icon = "Cog", Order = 6, RequiredPermission = Permissions.ParamConfig });
        registry.RegisterMenu(new MenuInfo { Title = "硬件配置", ViewId = "Hardware.ConfigView", Icon = "ServerNetwork", Order = 7, RequiredPermission = Permissions.HardwareConfig });

        return registry;
    }
}
