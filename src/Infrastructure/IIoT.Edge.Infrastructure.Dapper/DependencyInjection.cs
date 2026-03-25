using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using IIoT.Edge.Infrastructure.Dapper.Repository;
using IIoT.Edge.Infrastructure.Dapper.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Infrastructure.Dapper;

public static class DependencyInjection
{
    /// <summary>
    /// 注册 Dapper 基础设施
    /// 
    /// 在 App.xaml.cs 中调用：
    ///   var dapperDbDir = Path.Combine(appDataDir, "IIoT.Edge", "db");
    ///   services.AddDapperInfrastructure(dapperDbDir);
    /// </summary>
    public static IServiceCollection AddDapperInfrastructure(
        this IServiceCollection services,
        string dbDirectory)
    {
        // 连接工厂（单例）
        services.AddSingleton(new SqliteConnectionFactory(dbDirectory));

        // Store 注册（单例）
        // 同时注册为 IFailedRecordStore（业务层用）和 ITableInitializer（建表用）
        services.AddSingleton<FailedRecordStore>();
        services.AddSingleton<IFailedRecordStore>(sp => sp.GetRequiredService<FailedRecordStore>());
        services.AddSingleton<ITableInitializer>(sp => sp.GetRequiredService<FailedRecordStore>());

        // 将来新增的 Store 在这里追加，格式一样：
        // services.AddSingleton<LogQueueStore>();
        // services.AddSingleton<ILogQueueStore>(sp => sp.GetRequiredService<LogQueueStore>());
        // services.AddSingleton<ITableInitializer>(sp => sp.GetRequiredService<LogQueueStore>());

        return services;
    }

    /// <summary>
    /// 容器构建后调用：初始化所有已注册的表
    /// 
    /// 在 App.xaml.cs 中，BuildServiceProvider 之后调用：
    ///   await ServiceProvider.InitializeDapperTablesAsync();
    /// </summary>
    public static async Task InitializeDapperTablesAsync(
        this IServiceProvider serviceProvider)
    {
        var factory = serviceProvider.GetRequiredService<SqliteConnectionFactory>();
        var initializers = serviceProvider.GetServices<ITableInitializer>();

        // 按 DbName 分组，同一个 db 文件只开一次连接
        var groups = initializers.GroupBy(x => x.DbName);

        foreach (var group in groups)
        {
            using var connection = factory.Create(group.Key);

            foreach (var initializer in group)
            {
                await initializer.InitializeTableAsync(connection);
            }
        }
    }
}