using Dapper;
using IIoT.Edge.SharedKernel.DataPipeline;
using IIoT.Edge.SharedKernel.DataPipeline.DeviceLog;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Infrastructure.Persistence.Dapper.Connection;
using IIoT.Edge.Infrastructure.Persistence.Dapper.Repository;

namespace IIoT.Edge.Infrastructure.Persistence.Dapper.Stores;

/// <summary>
/// 设备日志离线缓冲 SQLite 实现
/// 
/// 对应数据库：pipeline.db
/// 对应表：device_log_buffer
/// 
/// 写入方：DeviceLogSyncTask（POST 失败或离线时）
/// 读取方：RetryTask[Cloud]（分批补传）
/// </summary>
public class DeviceLogBufferStore : DapperRepositoryBase<DeviceLogRecord>, IDeviceLogBufferStore
{
    public override string DbName => "pipeline";
    protected override string TableName => "device_log_buffer";

    protected override string CreateTableSql => @"
        CREATE TABLE IF NOT EXISTS device_log_buffer (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Level       TEXT    NOT NULL,
            Message     TEXT    NOT NULL,
            LogTime     TEXT    NOT NULL,
            CreatedAt   TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_device_log_buffer_id
            ON device_log_buffer (Id);
    ";

    public DeviceLogBufferStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    public async Task SaveBatchAsync(IEnumerable<DeviceLogRecord> records)
    {
        const string sql = @"
            INSERT INTO device_log_buffer (Level, Message, LogTime, CreatedAt)
            VALUES (@Level, @Message, @LogTime, @CreatedAt)";

        try
        {
            using var conn = GetConnection();
            await conn.ExecuteAsync(sql, records);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 批量写入失败 [{TableName}]: {ex.Message}");
        }
    }

    public async Task<List<DeviceLogRecord>> GetPendingAsync(int batchSize = 100)
    {
        var sql = $@"
            SELECT * FROM {TableName}
            ORDER BY Id ASC
            LIMIT @BatchSize";

        var result = await SafeQueryAsync(sql, new { BatchSize = batchSize });
        return result.ToList();
    }

    public async Task DeleteBatchAsync(IEnumerable<long> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        // SQLite 参数有限，分批删除
        foreach (var batch in ChunkBy(idList, 500))
        {
            var sql = $"DELETE FROM {TableName} WHERE Id IN ({string.Join(",", batch)})";
            await SafeExecuteAsync(sql);
        }
    }

    public async Task<int> GetCountAsync()
    {
        return await SafeCountAsync($"SELECT COUNT(*) FROM {TableName}");
    }

    private static IEnumerable<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
    {
        for (int i = 0; i < source.Count; i += chunkSize)
            yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
    }
}
