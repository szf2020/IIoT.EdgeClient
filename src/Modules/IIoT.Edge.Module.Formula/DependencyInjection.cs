// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Formula/DependencyInjection.cs
//
// 修改点：
// 1. RecipeViewWidget 构造注入由 IRecipeService + IAuthService 改为 ISender + IRecipeService
// 2. 由于 Formula 程序集不在 Shell AddMediatR 的扫描范围内，
//    必须在此手动注册 RecipeViewQueries.cs 中的所有 Handler

using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.Module.Formula.RecipeView;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Formula;

public static class DependencyInjection
{
    public static IServiceCollection AddFormulaModule(
        this IServiceCollection services)
    {
        // ── RecipeViewWidget ────────────────────────────────────────────────
        services.AddSingleton<RecipeViewWidget>();

        // ── RecipeView Query/Command Handlers（手动注册，Formula 程序集不在 MediatR 扫描范围）
        services.AddTransient<IRequestHandler<GetRecipeViewSnapshotQuery, RecipeViewSnapshot?>,
            GetRecipeViewSnapshotHandler>();
        services.AddTransient<IRequestHandler<GetIsLocalAdminQuery, bool>,
            GetIsLocalAdminHandler>();
        services.AddTransient<IRequestHandler<SyncRecipeFromCloudCommand, bool>,
            SyncRecipeFromCloudHandler>();
        services.AddTransient<IRequestHandler<SwitchRecipeSourceCommand>,
            SwitchRecipeSourceHandler>();
        services.AddTransient<IRequestHandler<SaveLocalRecipeParamCommand>,
            SaveLocalRecipeParamHandler>();
        services.AddTransient<IRequestHandler<DeleteLocalRecipeParamCommand>,
            DeleteLocalRecipeParamHandler>();

        return services;
    }
}
