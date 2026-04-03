// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Production/DependencyInjection.cs
//
// 修改点：
// 1. CapacityViewWidget 构造注入由 CapacityCloudQueryService 改为 ISender，
//    CapacityCloudQueryService 保留注册（Handler 注入用）
// 2. EquipmentWidget 不再作为 INotificationHandler 注册；
//    改为注册独立的 EquipmentCapacityUpdatedHandler
// 3. 新增注册 CapacityViewUpdatedHandler
// 4. MonitorWidget 构造注入由 IProductionContextStore 改为 ISender，DI 无变化
//
// 注意：Shell 的 AddMediatR 已扫描 Production 程序集
//       (RegisterServicesFromAssemblies 含 CellCompletedEventHandler 所在程序集)
//       因此 CapacityQueries / EquipmentQueries / MonitorQueries 的 Handler 全部自动注册，
//       此处无需手动注册 Handler。

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
        // ── CapacityView ────────────────────────────────────────────────────
        // CapacityCloudQueryService 保留（Handler 通过 primary constructor 注入）
        services.AddSingleton<CapacityCloudQueryService>();
        services.AddSingleton<CapacityViewWidget>();
        // 单独的 Notification Handler，替代原来让 ViewModel 直接实现 INotificationHandler
        services.AddSingleton<INotificationHandler<IIoT.Edge.Contracts.Events.CapacityUpdatedNotification>,
            CapacityViewUpdatedHandler>();

        // ── Equipment ───────────────────────────────────────────────────────
        services.AddSingleton<EquipmentWidget>();
        // 原来：services.AddSingleton<INotificationHandler<...>>(sp => sp.GetRequiredService<EquipmentWidget>())
        // 现在：单独的 Handler 类
        services.AddSingleton<INotificationHandler<IIoT.Edge.Contracts.Events.CapacityUpdatedNotification>,
            EquipmentCapacityUpdatedHandler>();

        // ── Monitor / DataView ──────────────────────────────────────────────
        services.AddSingleton<MonitorWidget>();
        services.AddSingleton<DataViewWidget>();

        return services;
    }
}
