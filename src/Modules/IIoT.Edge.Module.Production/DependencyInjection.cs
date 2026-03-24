using IIoT.Edge.Module.Production.CapacityView;
using IIoT.Edge.Module.Production.DataView;
using IIoT.Edge.Module.Production.Equipment;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Production;

public static class DependencyInjection
{
    public static IServiceCollection AddProductionModule(
        this IServiceCollection services)
    {
        services.AddSingleton<DataViewWidget>();
        services.AddSingleton<CapacityViewWidget>();
        services.AddSingleton<EquipmentWidget>();

        return services;
    }
}