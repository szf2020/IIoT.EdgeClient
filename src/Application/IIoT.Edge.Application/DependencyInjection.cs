using IIoT.Edge.Application.Features.Config.ParamView;
using IIoT.Edge.Application.Features.Formula.RecipeView;
using IIoT.Edge.Application.Features.Hardware.HardwareConfigView;
using IIoT.Edge.Application.Features.Production.CapacityView;
using IIoT.Edge.Application.Features.Production.DataView;
using IIoT.Edge.Application.Features.Production.Equipment;
using IIoT.Edge.Application.Features.Production.Monitor;
using IIoT.Edge.Application.Features.SysLog.LogView;
using IIoT.Edge.Application.Common.Tasks;
using IIoT.Edge.Application.Abstractions.Tasks;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Application;

/// <summary>
/// Application 层依赖注入扩展。
/// 负责注册应用层对外暴露的服务与页面用例入口。
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册 Edge 客户端 Application 层服务。
    /// </summary>
    public static IServiceCollection AddEdgeApplication(this IServiceCollection services)
    {
        // 注册已知的电芯数据工序类型（新增工序在此处追加）
        CellDataTypeRegistry.Register<InjectionCellData>("Injection");
        services.AddSingleton<CapacityCloudQueryService>();
        services.AddTransient<IParamViewCrudService, ParamViewCrudService>();
        services.AddTransient<IHardwareConfigCrudService, HardwareConfigCrudService>();
        services.AddTransient<IRecipeViewCrudService, RecipeViewCrudService>();
        services.AddTransient<ICapacityViewService, CapacityViewService>();
        services.AddTransient<IDataViewService, DataViewService>();
        services.AddTransient<IMonitorViewService, MonitorViewService>();
        services.AddTransient<IEquipmentPanelService, EquipmentPanelService>();
        services.AddTransient<ILogViewService, LogViewService>();
        services.AddSingleton<IBackgroundServiceCoordinator, BackgroundServiceCoordinator>();
        return services;
    }
}
