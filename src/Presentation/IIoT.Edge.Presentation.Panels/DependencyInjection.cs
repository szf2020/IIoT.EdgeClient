using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.Presentation.Panels.Features.Equipment;
using IIoT.Edge.Presentation.Panels.Features.SysLog;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Presentation.Panels;

/// <summary>
/// Panels 层依赖注入与停靠面板注册扩展。
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册右侧面板所需服务与视图模型。
    /// </summary>
    public static IServiceCollection AddPanelPresentation(this IServiceCollection services)
    {
        // 纯日志服务（log4net 写文件，不涉及 UI），供所有层使用
        services.AddSingleton<ILogService, Log4NetLogService>();

        // 日志展示服务（装饰器，增加 UI 同步），仅供 ViewModel 使用
        services.AddSingleton<LogDisplayService>();
        services.AddSingleton<ILogDisplayService>(sp => sp.GetRequiredService<LogDisplayService>());

        services.AddSingleton<EquipmentViewModel>();
        services.AddSingleton<LogViewModel>();
        return services;
    }

    /// <summary>
    /// 注册右侧停靠面板视图。
    /// </summary>
    public static IViewRegistry RegisterPanelViews(this IViewRegistry registry)
    {
        registry.RegisterAnchorable(
            new AnchorableInfo
            {
                Title = "设备信息",
                ContentId = "Core.Equipment",
                InitialPosition = AnchorablePosition.Right,
                IsVisible = true
            },
            typeof(EquipmentView),
            typeof(EquipmentViewModel));

        registry.RegisterAnchorable(
            new AnchorableInfo
            {
                Title = "系统日志",
                ContentId = "Core.SysLog",
                InitialPosition = AnchorablePosition.Right,
                IsVisible = true
            },
            typeof(LogView),
            typeof(LogViewModel));

        return registry;
    }
}
