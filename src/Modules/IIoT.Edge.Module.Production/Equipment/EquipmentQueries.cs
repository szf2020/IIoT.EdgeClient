// 新增文件
// 路径：src/Modules/IIoT.Edge.Module.Production/Equipment/EquipmentQueries.cs
//
// Query 层：Handler 禁止操作 UI，只返回数据快照。
// HardwareItemViewModel / RecipeParamViewModel 来自同目录已有的 Models/ 子目录，不重复定义。
// 该文件在 Production 程序集内，Shell AddMediatR 已扫描，无需额外注册。

using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.Events;
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.Module.Production.Equipment.Models;
using MediatR;

namespace IIoT.Edge.Module.Production.Equipment;

// ── 数据快照（Handler → ViewModel，纯数据，无 UI 依赖）────────────────────────

public record HardwareSnapshot(
    string Name,
    string Address,
    string DeviceType,
    bool   IsConnected);

public record RecipeSnapshot(
    string                     RecipeName,
    string                     RecipeVersion,
    string                     ProcessName,
    bool                       IsRecipeActive,
    List<RecipeParamViewModel> Parameters);

public record CapacitySnapshot(
    int    TodayOutput,
    int    NgCount,
    string TodayYield,
    string CurrentBatch);

// ── Queries ──────────────────────────────────────────────────────────────────

public record GetHardwareStatusQuery   : IRequest<List<HardwareSnapshot>>;
public record GetRecipeSnapshotQuery   : IRequest<RecipeSnapshot?>;
public record GetCapacitySnapshotQuery : IRequest<CapacitySnapshot>;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class GetHardwareStatusHandler(
    ISender               sender,
    IPlcConnectionManager plcManager,
    IPlcDataStore         dataStore)
    : IRequestHandler<GetHardwareStatusQuery, List<HardwareSnapshot>>
{
    public async Task<List<HardwareSnapshot>> Handle(
        GetHardwareStatusQuery request, CancellationToken ct)
    {
        var result = await sender.Send(new GetAllNetworkDevicesQuery(), ct);
        if (!result.IsSuccess || result.Value is null) return new();

        return result.Value.Select(device =>
        {
            bool connected = (plcManager.GetPlc(device.Id)?.IsConnected == true)
                          || dataStore.HasDevice(device.Id);
            return new HardwareSnapshot(
                device.DeviceName,
                device.IpAddress,
                device.DeviceType.ToString(),
                connected);
        }).ToList();
    }
}

public class GetRecipeSnapshotHandler(IRecipeService recipeService)
    : IRequestHandler<GetRecipeSnapshotQuery, RecipeSnapshot?>
{
    public Task<RecipeSnapshot?> Handle(
        GetRecipeSnapshotQuery request, CancellationToken ct)
    {
        var recipe = recipeService.CloudRecipe
                  ?? recipeService.LocalRecipe
                  ?? recipeService.ActiveRecipe;

        if (recipe is null) return Task.FromResult<RecipeSnapshot?>(null);

        string tag  = recipe == recipeService.CloudRecipe ? "云端"
                    : recipe == recipeService.LocalRecipe  ? "本地" : "";
        string name = string.IsNullOrEmpty(tag)
            ? recipe.RecipeName
            : $"{recipe.RecipeName}（{tag}）";

        var parms = recipe.Parameters.Values.Select(p => new RecipeParamViewModel
        {
            ParamName    = p.Name,
            CurrentValue = p.CustomValue ?? "--",
            MinValue     = p.Min?.ToString("G4") ?? "--",
            MaxValue     = p.Max?.ToString("G4") ?? "--",
            Unit         = p.Unit,
            WarnLow      = p.Min.HasValue ? (p.Min * 1.05)?.ToString("G4") ?? "--" : "--",
            WarnHigh     = p.Max.HasValue ? (p.Max * 0.95)?.ToString("G4") ?? "--" : "--"
        }).ToList();

        return Task.FromResult<RecipeSnapshot?>(
            new RecipeSnapshot(name, recipe.Version, recipe.ProcessName,
                               recipe.Status == "Active", parms));
    }
}

public class GetCapacitySnapshotHandler(IProductionContextStore contextStore)
    : IRequestHandler<GetCapacitySnapshotQuery, CapacitySnapshot>
{
    public Task<CapacitySnapshot> Handle(
        GetCapacitySnapshotQuery request, CancellationToken ct)
    {
        int ok = 0, ng = 0;
        string batch = "--";

        foreach (var ctx in contextStore.GetAll())
        {
            ok += ctx.TodayCapacity.OkAll;
            ng += ctx.TodayCapacity.NgAll;
            if (ctx.DeviceBag.TryGetValue("CurrentBatch", out var b) && b is string s)
                batch = s;
        }

        int total = ok + ng;
        string yld = total > 0 ? $"{ok * 100.0 / total:F1}%" : "0.0%";
        return Task.FromResult(new CapacitySnapshot(total, ng, yld, batch));
    }
}

// ── Notification Handler ──────────────────────────────────────────────────────
// ViewModel 禁止直接实现 INotificationHandler，由此单独类代理。

public class EquipmentCapacityUpdatedHandler(EquipmentWidget widget)
    : INotificationHandler<CapacityUpdatedNotification>
{
    public Task Handle(CapacityUpdatedNotification notification, CancellationToken ct)
    {
        widget.OnCapacityUpdated();
        return Task.CompletedTask;
    }
}
