using Dapper;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Infrastructure.Persistence.Dapper.Connection;
using System.Data;

namespace IIoT.Edge.Infrastructure.Persistence.Dapper.Repository;

public abstract class DapperRepositoryBase<TEntity> : ITableInitializer
    where TEntity : class
{
    protected readonly SqliteConnectionFactory ConnectionFactory;
    protected readonly ILogService Logger;

    protected abstract string TableName { get; }

    public abstract string DbName { get; }

    protected virtual int CommandTimeout => 30;

    protected DapperRepositoryBase(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
    {
        ConnectionFactory = connectionFactory;
        Logger = logger;
    }

    protected abstract string CreateTableSql { get; }

    public async Task InitializeTableAsync(IDbConnection connection)
    {
        try
        {
            await connection.ExecuteAsync(CreateTableSql);
            Logger.Info($"[Dapper] 表 {TableName} 初始化完成（{DbName}.db）");
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 表 {TableName} 初始化失败: {ex.Message}");
            throw;
        }
    }

    protected IDbConnection GetConnection() => ConnectionFactory.Create(DbName);

    protected Task<IDbConnection> GetConnectionAsync() => ConnectionFactory.CreateAsync(DbName);

    public async Task<TEntity?> GetByIdAsync(long id)
    {
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        return await SafeQueryFirstOrDefaultAsync(sql, new { Id = id });
    }

    protected async Task<IEnumerable<TEntity>> SafeQueryAsync(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<TEntity>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return Enumerable.Empty<TEntity>();
        }
    }

    protected async Task<TEntity?> SafeQueryFirstOrDefaultAsync(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<TEntity>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return default;
        }
    }

    protected async Task<int> SafeCountAsync(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<int>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 计数失败 [{TableName}]: {ex.Message}");
            return 0;
        }
    }

    protected async Task<int> SafeExecuteAsync(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 执行失败 [{TableName}]: {ex.Message}");
            return 0;
        }
    }

    protected async Task<long> SafeInsertReturnIdAsync(string sql, object param)
    {
        try
        {
            using var conn = GetConnection();
            var fullSql = $"{sql}; SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<long>(fullSql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 插入失败 [{TableName}]: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> DeleteByIdAsync(long id)
    {
        var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
        return await SafeExecuteAsync(sql, new { Id = id });
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action)
    {
        using var conn = GetConnection();
        using var transaction = conn.BeginTransaction();

        try
        {
            var result = await action(conn, transaction);
            transaction.Commit();
            return result;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Logger.Error($"[Dapper] 事务执行失败 [{TableName}]: {ex.Message}");
            throw;
        }
    }
}
