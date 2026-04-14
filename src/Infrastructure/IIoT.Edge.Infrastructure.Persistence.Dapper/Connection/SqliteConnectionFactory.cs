using Microsoft.Data.Sqlite;
using System.Data;

namespace IIoT.Edge.Infrastructure.Persistence.Dapper.Connection;

/// <summary>
/// SQLite 连接工厂
/// 
/// 统一管理所有轻量级 SQLite 数据库的连接创建
/// 所有 .db 文件集中存放在注入时指定的目录下
/// 
/// 使用方式：
///   using var conn = _factory.Create("pipeline");
///   // conn 指向 {dbDir}/pipeline.db
/// 
/// 连接特性：
///   - WAL 模式（并发读写性能好，适合队列场景）
///   - 连接用完由调用方 Dispose，工厂不持有连接
///   - 每次 Create 都是新连接，无连接池（SQLite 本地文件无需池化）
/// </summary>
public class SqliteConnectionFactory
{
    private readonly string _dbDirectory;

    /// <summary>
    /// db 文件存放目录（只读，供外部检查路径用）
    /// </summary>
    public string DbDirectory => _dbDirectory;

    public SqliteConnectionFactory(string dbDirectory)
    {
        if (string.IsNullOrWhiteSpace(dbDirectory))
            throw new ArgumentNullException(nameof(dbDirectory));

        _dbDirectory = dbDirectory;
        Directory.CreateDirectory(_dbDirectory);
    }

    /// <summary>
    /// 创建一个到指定数据库的连接（已打开，已启用 WAL）
    /// </summary>
    /// <param name="dbName">数据库名称（不含扩展名），如 "pipeline"、"logs"</param>
    /// <returns>已打开的 IDbConnection，调用方负责 Dispose</returns>
    public IDbConnection Create(string dbName)
    {
        var dbPath = Path.Combine(_dbDirectory, $"{dbName}.db");
        var connStr = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var connection = new SqliteConnection(connStr);
        connection.Open();

        // 启用 WAL 模式：允许读写并发，适合队列高频读写场景
        using var walCmd = connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();

        // 启用外键约束（虽然当前不用，但作为规范默认开启）
        using var fkCmd = connection.CreateCommand();
        fkCmd.CommandText = "PRAGMA foreign_keys=ON;";
        fkCmd.ExecuteNonQuery();

        return connection;
    }

    /// <summary>
    /// 异步版本
    /// </summary>
    public async Task<IDbConnection> CreateAsync(string dbName)
    {
        var dbPath = Path.Combine(_dbDirectory, $"{dbName}.db");
        var connStr = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var connection = new SqliteConnection(connStr);
        await connection.OpenAsync();

        await using var walCmd = connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        await walCmd.ExecuteNonQueryAsync();

        await using var fkCmd = connection.CreateCommand();
        fkCmd.CommandText = "PRAGMA foreign_keys=ON;";
        await fkCmd.ExecuteNonQueryAsync();

        return connection;
    }

    /// <summary>
    /// 获取指定数据库的完整文件路径（诊断/日志用）
    /// </summary>
    public string GetDbPath(string dbName)
        => Path.Combine(_dbDirectory, $"{dbName}.db");
}