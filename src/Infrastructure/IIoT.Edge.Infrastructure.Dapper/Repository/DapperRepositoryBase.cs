using Dapper;
using IIoT.Edge.Contracts;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using System.Data;

namespace IIoT.Edge.Infrastructure.Dapper.Repository;

/// <summary>
/// Dapper 仓储基类
/// 
/// 封装常用的 CRUD 操作，子类只需要：
///   1. 指定 TableName 和 DbName
///   2. 实现 ITableInitializer 提供建表 SQL
///   3. 写具体的业务查询方法（调基类的 QueryAsync / ExecuteAsync）
/// 
/// 所有数据库操作统一走这里，异常统一捕获记日志，不让底层异常裸抛
/// 每次操作独立获取连接、用完即释放，不持有长连接
/// </summary>
public abstract class DapperRepositoryBase<TEntity> : ITableInitializer
    where TEntity : class
{
    protected readonly SqliteConnectionFactory ConnectionFactory;
    protected readonly ILogService Logger;

    /// <summary>
    /// 表名（子类必须指定）
    /// </summary>
    protected abstract string TableName { get; }

    /// <summary>
    /// 数据库名称（子类必须指定，如 "pipeline"）
    /// </summary>
    public abstract string DbName { get; }

    /// <summary>
    /// SQL 命令超时时间（秒），子类可覆盖
    /// </summary>
    protected virtual int CommandTimeout => 30;

    protected DapperRepositoryBase(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
    {
        ConnectionFactory = connectionFactory;
        Logger = logger;
    }

    // ── 建表 ──────────────────────────────────────────

    /// <summary>
    /// 子类提供建表 SQL（CREATE TABLE IF NOT EXISTS ...）
    /// </summary>
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

    // ── 连接获取 ──────────────────────────────────────

    /// <summary>
    /// 获取数据库连接（调用方负责 Dispose）
    /// </summary>
    protected IDbConnection GetConnection()
        => ConnectionFactory.Create(DbName);

    protected Task<IDbConnection> GetConnectionAsync()
        => ConnectionFactory.CreateAsync(DbName);

    // ── 查询 ──────────────────────────────────────────

    /// <summary>
    /// 根据 Id 查询单条记录
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(long id)
    {
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        return await SafeQueryFirstOrDefaultAsync(sql, new { Id = id });
    }

    /// <summary>
    /// 查询多条记录
    /// </summary>
    protected async Task<IEnumerable<TEntity>> SafeQueryAsync(
        string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<TEntity>(
                sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return Enumerable.Empty<TEntity>();
        }
    }

    /// <summary>
    /// 查询单条记录
    /// </summary>
    protected async Task<TEntity?> SafeQueryFirstOrDefaultAsync(
        string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<TEntity>(
                sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 查询数量
    /// </summary>
    protected async Task<int> SafeCountAsync(
        string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<int>(
                sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 计数失败 [{TableName}]: {ex.Message}");
            return 0;
        }
    }

    // ── 写入 ──────────────────────────────────────────

    /// <summary>
    /// 执行写入/更新/删除
    /// </summary>
    protected async Task<int> SafeExecuteAsync(
        string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync(
                sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 执行失败 [{TableName}]: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 插入并返回自增Id
    /// </summary>
    protected async Task<long> SafeInsertReturnIdAsync(
        string sql, object param)
    {
        try
        {
            using var conn = GetConnection();
            // SQLite 用 last_insert_rowid() 获取自增Id
            var fullSql = $"{sql}; SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<long>(
                fullSql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 插入失败 [{TableName}]: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 根据 Id 删除
    /// </summary>
    public async Task<int> DeleteByIdAsync(long id)
    {
        var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
        return await SafeExecuteAsync(sql, new { Id = id });
    }

    // ── 事务 ──────────────────────────────────────────

    /// <summary>
    /// 在事务中执行多个操作
    /// 
    /// 用法：
    ///   await ExecuteInTransactionAsync(async (conn, tran) =>
    ///   {
    ///       await conn.ExecuteAsync(sql1, param1, tran);
    ///       await conn.ExecuteAsync(sql2, param2, tran);
    ///       return true;
    ///   });
    /// </summary>
    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> action)
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

    /// <summary>
    /// 无返回值的事务版本
    /// </summary>
    protected async Task ExecuteInTransactionAsync(
        Func<IDbConnection, IDbTransaction, Task> action)
    {
        await ExecuteInTransactionAsync<object?>(async (conn, tran) =>
        {
            await action(conn, tran);
            return null;
        });
    }
}