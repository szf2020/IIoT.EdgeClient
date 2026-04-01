// 路径：src/Modules/IIoT.Edge.Module.Production/DependencyInjection.cs
using IIoT.Edge.Module.Production.CapacityView;
using IIoT.Edge.Module.Production.DataView;
using IIoT.Edge.Module.Production.Equipment;
using IIoT.Edge.Module.Production.Monitor;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Production;

public static class DependencyInjection
{
    public static IServiceCollection AddProductionModule(
        this IServiceCollection services)
    {
        services.AddSingleton<DataViewWidget>();
        services.AddSingleton<CapacityViewWidget>();
        services.AddSingleton<MonitorWidget>();

        // EquipmentWidget 同时作为 MediatR INotificationHandler 注册
        services.AddSingleton<EquipmentWidget>();
        services.AddSingleton<INotificationHandler<IIoT.Edge.Contracts.Events.CapacityUpdatedNotification>>(
            sp => sp.GetRequiredService<EquipmentWidget>());

        return services;
    }
}