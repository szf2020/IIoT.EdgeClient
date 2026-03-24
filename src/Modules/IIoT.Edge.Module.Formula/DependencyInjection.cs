using IIoT.Edge.Module.Formula.RecipeView;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.Formula;

public static class DependencyInjection
{
    public static IServiceCollection AddFormulaModule(
        this IServiceCollection services)
    {
        services.AddSingleton<RecipeViewWidget>();

        return services;
    }
}