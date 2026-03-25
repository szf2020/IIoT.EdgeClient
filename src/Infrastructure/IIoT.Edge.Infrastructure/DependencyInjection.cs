using IIoT.Edge.Common.Repository;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Infrastructure.Cache;
using IIoT.Edge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string dbPath)
    {
        services.AddDbContextFactory<EdgeDbContext>(
            options =>
                options.UseSqlite(
                    $"Data Source={dbPath}"));

        services.AddSingleton(typeof(IReadRepository<>),
            typeof(EfReadRepository<>));
        services.AddSingleton(typeof(IRepository<>),
            typeof(EfRepository<>));

        // 通用缓存
        services.AddSingleton<IEdgeCacheService,
            EdgeCacheService>();


        return services;
    }

    public static void ApplyMigrations(
        this IServiceProvider serviceProvider)
    {
        var factory = serviceProvider
            .GetRequiredService<
                IDbContextFactory<EdgeDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }
}