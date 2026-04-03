// 新增文件
// 路径：src/Modules/IIoT.Edge.Module.Formula/RecipeView/RecipeViewQueries.cs
//
// Query 层：Handler 禁止操作 UI，只返回数据。
// RecipeParamVm 来自同目录已有的 RecipeViewWidget.cs 末尾，不重复定义。
//
// 注意：Formula 程序集不在 Shell AddMediatR 的扫描范围内。
// 需在 Formula 的 DependencyInjection.cs 中手动注册这些 Handler（见对应 DI 文件）。

using IIoT.Edge.Common.DataPipeline.Recipe;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.Recipe;
using MediatR;

namespace IIoT.Edge.Module.Formula.RecipeView;

// ── 快照（Handler → ViewModel）───────────────────────────────────────────────

public record RecipeViewSnapshot(
    string              RecipeName,
    string              RecipeVersion,
    string              ProcessName,
    string              UpdatedAt,
    bool                IsCloudSource,
    List<RecipeParamVm> Params);

// ── Queries ──────────────────────────────────────────────────────────────────

public record GetRecipeViewSnapshotQuery : IRequest<RecipeViewSnapshot?>;
public record GetIsLocalAdminQuery       : IRequest<bool>;

// ── Commands ─────────────────────────────────────────────────────────────────

public record SyncRecipeFromCloudCommand                      : IRequest<bool>;
public record SwitchRecipeSourceCommand(RecipeSource Source)  : IRequest;
public record SaveLocalRecipeParamCommand(
    string Key, double? Min, double? Max, string Unit)        : IRequest;
public record DeleteLocalRecipeParamCommand(string Key)       : IRequest;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class GetRecipeViewSnapshotHandler(IRecipeService recipeService)
    : IRequestHandler<GetRecipeViewSnapshotQuery, RecipeViewSnapshot?>
{
    public Task<RecipeViewSnapshot?> Handle(
        GetRecipeViewSnapshotQuery request, CancellationToken ct)
    {
        var recipe = recipeService.ActiveRecipe;
        if (recipe is null) return Task.FromResult<RecipeViewSnapshot?>(null);

        var parms = recipe.Parameters
            .OrderBy(kv => kv.Key)
            .Select(kv => new RecipeParamVm
            {
                Name = kv.Value.Name,
                Min  = kv.Value.Min?.ToString("F2") ?? "--",
                Max  = kv.Value.Max?.ToString("F2") ?? "--",
                Unit = kv.Value.Unit
            })
            .ToList();

        return Task.FromResult<RecipeViewSnapshot?>(new RecipeViewSnapshot(
            recipe.RecipeName,
            recipe.Version,
            recipe.ProcessName,
            recipe.UpdatedAt,
            recipeService.ActiveSource == RecipeSource.Cloud,
            parms));
    }
}

public class GetIsLocalAdminHandler(IAuthService authService)
    : IRequestHandler<GetIsLocalAdminQuery, bool>
{
    public Task<bool> Handle(GetIsLocalAdminQuery request, CancellationToken ct)
        => Task.FromResult(authService.CurrentUser?.IsLocalAdmin ?? false);
}

public class SyncRecipeFromCloudHandler(IRecipeService recipeService)
    : IRequestHandler<SyncRecipeFromCloudCommand, bool>
{
    public Task<bool> Handle(SyncRecipeFromCloudCommand request, CancellationToken ct)
        => recipeService.PullFromCloudAsync();
}

public class SwitchRecipeSourceHandler(IRecipeService recipeService)
    : IRequestHandler<SwitchRecipeSourceCommand>
{
    public Task Handle(SwitchRecipeSourceCommand request, CancellationToken ct)
    {
        recipeService.SwitchSource(request.Source);
        return Task.CompletedTask;
    }
}

public class SaveLocalRecipeParamHandler(IRecipeService recipeService)
    : IRequestHandler<SaveLocalRecipeParamCommand>
{
    public Task Handle(SaveLocalRecipeParamCommand request, CancellationToken ct)
    {
        recipeService.SetLocalParam(request.Key, request.Min, request.Max, request.Unit);
        return Task.CompletedTask;
    }
}

public class DeleteLocalRecipeParamHandler(IRecipeService recipeService)
    : IRequestHandler<DeleteLocalRecipeParamCommand>
{
    public Task Handle(DeleteLocalRecipeParamCommand request, CancellationToken ct)
    {
        recipeService.RemoveLocalParam(request.Key);
        return Task.CompletedTask;
    }
}
